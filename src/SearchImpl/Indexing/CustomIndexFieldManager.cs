using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Documents;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

namespace SenseNet.Search.Indexing
{
    public interface IHasCustomIndexField { }
    public interface ICustomIndexFieldProvider
    {
        IEnumerable<Fieldable> GetFields(IndexDocumentData docData);
    }
    internal class CustomIndexFieldManager
    {
        internal static IEnumerable<Fieldable> GetFields(IndexDocument indexDocument, IndexDocumentData indexDocumentData)
        {
            Debug.WriteLine("%> adding custom fields for " + indexDocumentData.Path);
            return Instance.GetFieldsPrivate(indexDocumentData);
        }

        // -------------------------------------------------------------

        private static readonly object InstanceSync = new object();
        private static CustomIndexFieldManager _instance;
        private static CustomIndexFieldManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceSync)
                    {
                        if (_instance == null)
                        {
                            _instance = new CustomIndexFieldManager
                            {
                                _providers = TypeResolver.GetTypesByInterface(typeof(ICustomIndexFieldProvider))
                                    .Select(t => (ICustomIndexFieldProvider) Activator.CreateInstance(t)).ToArray()
                            };
                        }
                    }
                }
                return _instance;
            }
        }

        // ---------------------------------------------------------------------

        private IEnumerable<ICustomIndexFieldProvider> _providers;

        private CustomIndexFieldManager() { }

        private IEnumerable<Fieldable> GetFieldsPrivate(IndexDocumentData indexDocumentData)
        {
            var fields = new List<Fieldable>();
            foreach (var provider in _providers)
            {
                var f = provider.GetFields(indexDocumentData);
                if (f != null)
                    fields.AddRange(f);
            }
            return fields.Count == 0 ? null : fields;
        }

    }
}
