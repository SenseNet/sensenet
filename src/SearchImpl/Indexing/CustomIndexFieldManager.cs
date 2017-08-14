using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Tools;

namespace SenseNet.Search.Indexing
{
    public interface IHasCustomIndexField { }
    public interface ICustomIndexFieldProvider
    {
        IEnumerable<Fieldable> GetFields(SenseNet.ContentRepository.Storage.Data.IndexDocumentData docData);
    }
    internal class CustomIndexFieldManager
    {
        internal static IEnumerable<Fieldable> GetFields(IndexDocumentInfo info, SenseNet.ContentRepository.Storage.Data.IndexDocumentData docData)
        {
            Debug.WriteLine("%> adding custom fields for " + docData.Path);
            return Instance.GetFieldsPrivate(info, docData);
        }

        // -------------------------------------------------------------

        private static object _instanceSync = new object();
        private static CustomIndexFieldManager __instance;
        private static CustomIndexFieldManager Instance
        {
            get
            {
                if (__instance == null)
                {
                    lock (_instanceSync)
                    {
                        if (__instance == null)
                        {
                            var instance = new CustomIndexFieldManager();
                            instance._providers = TypeResolver.GetTypesByInterface(typeof(ICustomIndexFieldProvider))
                                .Select(t => (ICustomIndexFieldProvider)Activator.CreateInstance(t)).ToArray();
                            __instance = instance;
                        }
                    }
                }
                return __instance;
            }
        }

        // ---------------------------------------------------------------------

        private IEnumerable<ICustomIndexFieldProvider> _providers;

        private CustomIndexFieldManager() { }

        private IEnumerable<Fieldable> GetFieldsPrivate(IndexDocumentInfo info, IndexDocumentData docData)
        {
            var fields = new List<Fieldable>();
            foreach (var provider in _providers)
            {
                var f = provider.GetFields(docData);
                if (f != null)
                    fields.AddRange(f);
            }
            return fields.Count == 0 ? null : fields;
        }

    }
}
