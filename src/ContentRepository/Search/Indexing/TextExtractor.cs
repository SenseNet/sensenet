using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using iTextSharp.text.pdf;
using Ionic.Zip;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Defines a context sensitive object for text extracting operations.
    /// </summary>
    public class TextExtractorContext
    {
        /// <summary>
        /// Initialize a new instance of the TextExtractorContext.
        /// </summary>
        /// <param name="versionId"></param>
        public TextExtractorContext(int versionId)
        {
            VersionId = versionId;
        }

        /// <summary>
        /// Gets the VersionId of the context.
        /// </summary>
        public int VersionId { get; }
    }

    /// <summary>
    /// Defines a text extractor interface.
    /// </summary>
    public interface ITextExtractor
    {
        /// <summary>
        /// Extracts all relevant text information from the passed stream. Do not catch any exception but throw if it is needed.
        /// </summary>
        /// <param name="stream">Input stream</param>
        /// <param name="context">Content information (e.g. version id)</param>
        /// <returns>Extracted text</returns>
        string Extract(Stream stream, TextExtractorContext context);
        /// <summary>
        /// If the text extractor is considered slow, it will be executed outside of the
        /// main indexing database transaction to make the database server more responsive.
        /// It will mean an additional database request when the extracting is finished.
        /// </summary>
        bool IsSlow { get; }
    }

    /// <summary>
    /// Implements the <see cref="ITextExtractor"/> for text extracting operations.
    /// </summary>
    public abstract class TextExtractor : ITextExtractor
    {
        /// <inheritdoc />
        public abstract string Extract(Stream stream, TextExtractorContext context);
        /// <inheritdoc />
        public virtual bool IsSlow => true;

        private static ITextExtractor ResolveExtractor(BinaryData binaryData)
        {
            if (binaryData == null)
                return null;
            var fname = binaryData.FileName;
            if (string.IsNullOrEmpty(fname))
                return null;
            var ext = fname.Extension;
            if (string.IsNullOrEmpty(ext))
                return null;

            return ResolveExtractor(ext);
        }

        private static ITextExtractor ResolveExtractor(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var extractors = Settings.GetValue<ReadOnlyDictionary<string, ITextExtractor>>(
                IndexingSettings.SettingsName, IndexingSettings.TextExtractorsPropertyName);

            if (extractors == null)
                return null;

            if (extractors.TryGetValue(name.ToLower(), out var extractor))
                return extractor;

            return null;
        }

        /// <summary>
        /// Returns with the text extract of the given binaryData of the node.
        /// </summary>
        /// <param name="binaryData"><see cref="BinaryData"/> that will be extracted.</param>
        /// <param name="node">Owner <see cref="Node"/>.</param>
        /// <returns></returns>
        public static string GetExtract(BinaryData binaryData, Node node)
        {
            using (var op = SnTrace.Index.StartOperation("Getting text extract, VId:{0}, Path:{1}", node.VersionId, node.Path))
            {
                var extractor = ResolveExtractor(binaryData);
                if (extractor == null)
                {
                    op.Successful = true;
                    return string.Empty;
                }

                var result = string.Empty;

                using (var stream = binaryData.GetStream())
                {
                    if (stream == null || stream.Length == 0)
                    {
                        op.Successful = true;
                        return string.Empty;
                    }

                    try
                    {
                        var ctx = new TextExtractorContext(node.VersionId);
                        // async
                        void TimeboxedFunctionCall(TimeboxedActivity activity)
                        {
                            var x = (Stream) activity.InArgument;
                            var extract = extractor.Extract(x, ctx);
                            activity.OutArgument = extract;
                        }

                        var act = new TimeboxedActivity
                        {
                            InArgument = stream,
                            Activity = TimeboxedFunctionCall,
                            Context = HttpContext.Current
                        };

                        var finishedWithinTime = act.ExecuteAndWait(Configuration.Indexing.TextExtractTimeout * 1000);
                        if (!finishedWithinTime)
                        {
                            act.Abort();
                            var msg = $"Text extracting timeout. Version: {node.Version}, path: {node.Path}";
                            SnTrace.Index.Write(msg);
                            SnLog.WriteWarning(msg);
                            op.Successful = true;
                            return string.Empty;
                        }
                        else if (act.ExecutionException != null)
                        {
                            WriteError(act.ExecutionException, node);
                        }
                        else
                        {
                            result = (string)act.OutArgument;
                        }
                    }
                    catch (Exception e)
                    {
                        WriteError(e, node);
                    }
                }

                if (result == null)
                    SnLog.WriteWarning(string.Format(CultureInfo.InvariantCulture, @"Couldn't extract text. VersionId: {0}, path: '{1}' ", node.VersionId, node.Path));
                else
                    result = result.Replace('\0', '.');

                if (result == null)
                    SnTrace.Index.Write("Couldn't extract text");
                else
                    SnTrace.Index.Write("Extracted length length: {0}.", result.Length);

                op.Successful = true;
                return result;
            }
        }

        /// <summary>
        /// Returns true if the text extraction maybe slow.
        /// </summary>
        /// <param name="binaryData"></param>
        /// <returns></returns>
        public static bool TextExtractingWillBePotentiallySlow(BinaryData binaryData)
        {
            var extractor = ResolveExtractor(binaryData);
            if (extractor == null)
                return false;
            return extractor.IsSlow;
        }

        private static void WriteError(Exception e, Node node)
        {
            if (e != null)
            {
                SnLog.WriteException(e,
                    node != null
                        ? $"An error occured during extracting text. Version: {node.Version}, path: {node.Path}"
                        : "An error occured during extracting text.");
            }
            else
            {
                SnLog.WriteError(node != null
                    ? $"An error occured during extracting text. Version: {node.Version}, path: {node.Path}"
                    : "An error occured during extracting text.");
            }
        }

        /// <summary>
        /// Extracts text from the given stream that contains the content of the open xml file.
        /// </summary>
        protected string GetOpenXmlText(Stream stream, TextExtractorContext context)
        {
            var result = new StringBuilder();
            using (var zip = ZipFile.Read(stream))
            {
                foreach (var entry in zip)
                {
                    if (Path.GetExtension(entry.FileName.ToLower()).Trim('.') == "xml")
                    {
                        var zipStream = new MemoryStream();
                        entry.Extract(zipStream);
                        zipStream.Seek(0, SeekOrigin.Begin);

                        // use the XML extractor for inner entries in OpenXml files
                        var extractor = ResolveExtractor("xml");
                        var extractedText = extractor?.Extract(zipStream, context);

                        if (string.IsNullOrEmpty(extractedText))
                        {
                            zipStream.Close();
                            continue;
                        }
                        result.Append(extractedText);
                        zipStream.Close();
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Reads the whole given stream into a byte[] buffer and returns with it.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected static byte[] GetBytesFromStream(Stream stream)
        {
            byte[] fileData;
            if (stream is MemoryStream memoryStream)
            {
                fileData = memoryStream.ToArray();
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    fileData = ms.ToArray();
                }
            }

            return fileData;
        }
    }

    internal sealed class DocxTextExtractor : TextExtractor
    {
        public override string Extract(Stream stream, TextExtractorContext context)
        {
            return GetOpenXmlText(stream, context);
        }
    }
    internal sealed class XlsxTextExtractor : TextExtractor
    {
        public override string Extract(Stream stream, TextExtractorContext context)
        {
            return GetOpenXmlText(stream, context);
        }
    }
    internal sealed class PptxTextExtractor : TextExtractor
    {
        public override string Extract(Stream stream, TextExtractorContext context)
        {
            return GetOpenXmlText(stream, context);
        }
    }
    internal sealed class PdfTextExtractor : TextExtractor
    {
        private static bool _iFilterErrorLogged;

        public override string Extract(Stream stream, TextExtractorContext context)
        {
            try
            {
                // extract text using IFilter
                return SnIFilter.GetText(stream, ".pdf");
            }
            catch (OutOfMemoryException ex)
            {
                SnLog.WriteWarning("Pdf text extract failed with out of memory exception. " + ex,
                    EventId.Indexing,
                    properties: new Dictionary<string, object> {{"Stream size", stream.Length}});

                return string.Empty;
            }
            catch (Exception ex)
            {
                // log iFilter error only once
                if (!_iFilterErrorLogged)
                {
                    SnLog.WriteWarning("Pdf IFilter error: " + ex.Message, EventId.Indexing);
                    _iFilterErrorLogged = true;
                }
            }

            // fallback to the other mechanism in case the pdf IFilter is missing
            var text = new StringBuilder();

            try
            {
                var pdfReader = new PdfReader(stream);
                for (var page = 1; page <= pdfReader.NumberOfPages; page++)
                {
                    // extract text using the old version (4.1.6) of iTextSharp
                    var pageText = ExtractTextFromPdfBytes(pdfReader.GetPageContent(page));
                    if (string.IsNullOrEmpty(pageText))
                        continue;

                    text.Append(pageText);
                }
            }
            catch (OutOfMemoryException ex)
            {
                SnLog.WriteWarning("Pdf text extract failed with out of memory exception. " + ex,
                    EventId.Indexing,
                    properties: new Dictionary<string, object> {{"Stream size", stream.Length}});
            }

            return text.ToString();
        }

        /// <summary>
        /// Old algorithm designed to work with iTextSharp 4.1.6. Use iTextSharp version >= 5 if possible (license changes were made).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static string ExtractTextFromPdfBytes(byte[] input)
        {
            if (input == null || input.Length == 0)
                return "";

            var result = new StringBuilder();
            var tokeniser = new PRTokeniser(input);

            try
            {
                while (tokeniser.NextToken())
                {
                    var tknType = tokeniser.TokenType;
                    var tknValue = tokeniser.StringValue.Replace('\0', ' ');

                    if (tknType == PRTokeniser.TK_STRING)
                    {
                        result.Append(tknValue);
                    }
                    else
                    {
                        switch (tknValue)
                        {
                            case "-600":
                                result.Append(" ");
                                break;
                            case "TJ":
                                result.Append(" ");
                                break;
                        }
                    }
                }
            }
            finally
            {
                tokeniser.Close();
            }

            return result.ToString();
        }
    }
    internal sealed class XmlTextExtractor : TextExtractor
    {
        public override bool IsSlow => false;

        public override string Extract(Stream stream, TextExtractorContext context)
        {
            // IMPORTANT: as this extractor is used for extracting text from inner
            // entries of OpenXml files, please do not make this method asynchronous,
            // because we cannot assume that the file is a real content in the
            // Content Repository.

            // initial length: chars = bytes / 2, relevant text rate: ~25%
            var sb = new StringBuilder(Math.Max(20, Convert.ToInt32(stream.Length / 8)));
            var reader = new XmlTextReader(stream);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Text && reader.HasValue)
                {
                    sb.Append(reader.Value).Append(' ');
                }
            }

            return sb.ToString();
        }
    }
    internal sealed class DocTextExtractor : TextExtractor
    {
        public override string Extract(Stream stream, TextExtractorContext context)
        {
            try
            {
                // IFilter
                return SnIFilter.GetText(stream, ".doc");
            }
            catch (Exception ex)
            {
                SnLog.WriteWarning("Doc IFilter error: " + ex.Message, EventId.Indexing);
            }

            return string.Empty;
        }
    }
    internal sealed class XlsTextExtractor : TextExtractor
    {
        public override string Extract(Stream stream, TextExtractorContext context)
        {
            try
            {
                // IFilter
                return SnIFilter.GetText(stream, ".xls");
            }
            catch (Exception ex)
            {
                SnLog.WriteWarning("Xls IFilter error: " + ex.Message, EventId.Indexing);
            }

            return string.Empty;
        }
    }
    internal sealed class XlbTextExtractor : TextExtractor
    {
        public override string Extract(Stream stream, TextExtractorContext context)
        {
            try
            {
                // IFilter
                return SnIFilter.GetText(stream, ".xlb");
            }
            catch (Exception ex)
            {
                SnLog.WriteWarning("Xlb IFilter error: " + ex.Message, EventId.Indexing);
            }

            return string.Empty;
        }
    }
    internal sealed class MsgTextExtractor : TextExtractor
    {
        public override string Extract(Stream stream, TextExtractorContext context)
        {
            try
            {
                // IFilter
                return SnIFilter.GetText(stream, ".msg");
            }
            catch (Exception ex)
            {
                SnLog.WriteWarning("Msg IFilter error: " + ex.Message, EventId.Indexing);
            }

            return string.Empty;
        }
    }
    internal sealed class PlainTextExtractor : TextExtractor
    {
        public override bool IsSlow => false;

        public override string Extract(Stream stream, TextExtractorContext context)
        {
            return RepositoryTools.GetStreamString(stream);
        }
    }
    internal sealed class RtfTextExtractor : TextExtractor
    {
        public override bool IsSlow => false;

        public override string Extract(Stream stream, TextExtractorContext context)
        {
            return RichTextStripper.StripRichTextFormat(RepositoryTools.GetStreamString(stream));
        }
    }

    /// <summary>
    /// A text extractor that does nothing: it returns with an empty text. It was created
    /// for cases when a built-in extractor does not work and needs to be switched off. This 
    /// can be done by seting this technical extractor class for an extension.
    /// </summary>
    internal sealed class NullTextExtractor : TextExtractor
    {
        public override bool IsSlow => false;

        public override string Extract(Stream stream, TextExtractorContext context)
        {
            return string.Empty;
        }
    }
}
