using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.Search
{
    [ContentHandler]
    public class IndexingSettings : Settings
    {
        // ================================================================================= Constructors

        public IndexingSettings(Node parent) : this(parent, null) { }
        public IndexingSettings(Node parent, string nodeTypeName) : base(parent, nodeTypeName) {}
        protected IndexingSettings(NodeToken nt) : base(nt) { }

        // ================================================================================= Properties

        private const string TEXTEXTRACTORS_TEXTFIELDNAME = "TextExtractors";
        internal const string TEXTEXTRACTORS_PROPERTYNAME = "TextExtractorInstances";
        internal const string SETTINGSNAME = "Indexing";

        private static readonly object ExtractorLock = new object();
        private ReadOnlyDictionary<string, ITextExtractor> _textExtractors;
        public ReadOnlyDictionary<string, ITextExtractor> TextExtractorInstances
        {
            get
            {
                if (_textExtractors == null)
                {
                    lock (ExtractorLock)
                    {
                        if (_textExtractors == null)
                        {
                            _textExtractors = new ReadOnlyDictionary<string, ITextExtractor>(LoadTextExtractors());

                            SetCachedData(TEXTEXTRACTORS_CACHEKEY, _textExtractors);

                            SnLog.WriteInformation("Text extractors were created.", properties: _textExtractors.ToDictionary(t => t.Key, t => (object)t.Value.GetType().FullName));
                        }
                    }
                }

                return _textExtractors;
            }
        }

        /// <summary>
        /// This method collects all the available text extractors. First it builds a list of the built-in
        /// extractors than adds the dynamic ones from the setting (they can override the built-in ones).
        /// </summary>
        private Dictionary<string, ITextExtractor> LoadTextExtractors()
        {
            var extractors = new Dictionary<string, ITextExtractor>
                            {
                                {"contenttype", new XmlTextExtractor()},
                                {"xml", new XmlTextExtractor()},
                                {"doc", new DocTextExtractor()},
                                {"xls", new XlsTextExtractor()},
                                {"xlb", new XlbTextExtractor()},
                                {"msg", new MsgTextExtractor()},
                                {"pdf", new PdfTextExtractor()},
                                {"docx", new DocxTextExtractor()},
                                {"docm", new DocxTextExtractor()},
                                {"xlsx", new XlsxTextExtractor()},
                                {"xlsm", new XlsxTextExtractor()},
                                {"pptx", new PptxTextExtractor()},
                                {"txt", new PlainTextExtractor()},
                                {"settings", new PlainTextExtractor()},
                                {"rtf", new RtfTextExtractor()}
                            };

            // load text extractor settings (they may override the defaults listed above)
            foreach (var field in this.Content.Fields.Values.Where(field => field.Name.StartsWith(TEXTEXTRACTORS_TEXTFIELDNAME + ".")))
            {
                var extractorName = field.GetData() as string;
                if (string.IsNullOrEmpty(extractorName))
                    continue;

                extractorName = extractorName.Trim('.', ' ');

                try
                {
                    var extension = field.Name.Substring(field.Name.LastIndexOf('.')).Trim('.', ' ').ToLower();
                    extractors[extension] = (ITextExtractor)TypeResolver.CreateInstance(extractorName);
                }
                catch (Exception ex)
                {
                    SnLog.WriteWarning($"Text extractor type could not be instatiated: {extractorName} {ex}", EventId.Indexing);
                }
            }

            return extractors;
        }

        // ================================================================================= Overrides

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case TEXTEXTRACTORS_PROPERTYNAME:
                    return this.TextExtractorInstances;
                default:
                    return base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case TEXTEXTRACTORS_PROPERTYNAME:
                    // this is a readonly property
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        // ================================================================================= Cached data

        private const string TEXTEXTRACTORS_CACHEKEY = "CachedTextExtractors";

        protected override void OnLoaded(object sender, NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            _textExtractors = (ReadOnlyDictionary<string, ITextExtractor>)GetCachedData(TEXTEXTRACTORS_CACHEKEY);
        }
    }
}
