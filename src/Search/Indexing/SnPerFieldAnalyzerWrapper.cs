using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.Tools;

namespace SenseNet.Search.Indexing
{
    /// <summary>
    /// Sense/Net specific Lucene analyzer, equivalent of Lucene's PerFieldAnalyzerWrapper.
    /// </summary>
    internal class SnPerFieldAnalyzerWrapper : Analyzer
    {
        private Analyzer _defaultAnalyzer;
        private Dictionary<string, Analyzer> _analyzers = new Dictionary<string, Analyzer>();

        private Analyzer GetAnalyzer(string fieldName)
        {
            // Hard-code the _Text field
            if (fieldName == "_Text")
                return _analyzers[typeof(StandardAnalyzer).FullName];

            // For everything else, ask the ContentTypeManager
            var pfii = ContentTypeManager.GetPerFieldIndexingInfo(fieldName);

            // Return the default analyzer if indexing info was not found.
            if (pfii == null)
                return _defaultAnalyzer;

            // Get analyzername by IndexFieldHandler
            string analyzerName = pfii.Analyzer;
            if (string.IsNullOrEmpty(analyzerName))
                analyzerName = pfii.IndexFieldHandler.GetDefaultAnalyzerName();

            // Return the default analyzer if it is not specified any way.
            if (string.IsNullOrEmpty(analyzerName))
                return _defaultAnalyzer;

            Analyzer analyzer;
            if (_analyzers.TryGetValue(analyzerName, out analyzer))
                return analyzer;

            // Find the type
            var analyzerType = TypeResolver.GetType(analyzerName);

            // If it doesn't exist, return the default analyzer
            if (analyzerType == null)
                return _defaultAnalyzer;

            // Store the instance in cache
            analyzer = (Analyzer)Activator.CreateInstance(analyzerType);
            _analyzers[analyzerName] = analyzer;

            // Return analyzer
            return analyzer;
        }

        /// <summary>Constructs with default analyzer.</summary>
        /// <param name="defaultAnalyzer">Any fields not specifically defined to use a different analyzer will use the one provided here.</param>
        public SnPerFieldAnalyzerWrapper(Analyzer defaultAnalyzer)
        {
            // Save default analyzer
            _defaultAnalyzer = defaultAnalyzer;
            // Add default analyzer type to cache
            _analyzers[defaultAnalyzer.GetType().FullName] = defaultAnalyzer;

            // Add standard analyzer to cache if it's not the default
            if (!_analyzers.ContainsKey(typeof(StandardAnalyzer).FullName))
                _analyzers[typeof(StandardAnalyzer).FullName] = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);
        }

        /// <summary>Defines an analyzer to use for the specified field.</summary>
        /// <param name="fieldName">field name requiring a non-default analyzer</param>
        /// <param name="analyzer">non-default analyzer to use for field</param>
        public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
        {
            var analyzer = GetAnalyzer(fieldName);
            return analyzer.TokenStream(fieldName, reader);
        }

        public override TokenStream ReusableTokenStream(System.String fieldName, System.IO.TextReader reader)
        {
            if (overridesTokenStreamMethod)
            {
                // LUCENE-1678: force fallback to tokenStream() if we
                // have been subclassed and that subclass overrides
                // tokenStream but not reusableTokenStream
                return TokenStream(fieldName, reader);
            }

            var analyzer = GetAnalyzer(fieldName);
            return analyzer.ReusableTokenStream(fieldName, reader);
        }

        /// <summary>Returns the positionIncrementGap from the analyzer assigned to fieldName </summary>
        public override int GetPositionIncrementGap(System.String fieldName)
        {
            var analyzer = GetAnalyzer(fieldName);
            return analyzer.GetPositionIncrementGap(fieldName);
        }

        /// <summary>Returns the offsetGap from the analyzer assigned to fiel</summary>
        public override int GetOffsetGap(Lucene.Net.Documents.Fieldable field)
        {
            var analyzer = GetAnalyzer(field.Name());
            return analyzer.GetOffsetGap(field);
        }

        public override System.String ToString()
        {
            // {{Aroush-2.9}} will 'analyzerMap.ToString()' work in the same way as Java's java.util.HashMap.toString()? 
            return "SnPerFieldAnalyzerWrapper(" + _analyzers.ToString() + ", default=" + _defaultAnalyzer + ")";
        }
    }
}
