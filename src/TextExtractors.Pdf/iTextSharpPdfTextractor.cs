using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using iTextSharp.text.pdf;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.Diagnostics;

namespace SenseNet.TextExtractors.Pdf
{
    // ReSharper disable once InconsistentNaming
    public class iTextSharpPdfTextExtractor : PdfTextExtractor
    {
        public override string Extract(Stream stream, TextExtractorContext context)
        {
            // call base method for iFilter logic
            var textExtract = ExtractiFilter(stream, out var success);
            if (success)
                return textExtract;

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
                    properties: new Dictionary<string, object> { { "Stream size", stream.Length } });
            }

            return text.ToString();
        }

        /// <summary>
        /// Old algorithm designed to work with iTextSharp 4.1.6. Use iTextSharp version >= 5 if possible (license changes were made).
        /// </summary>
        /// <param name="input"></param>
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
}
