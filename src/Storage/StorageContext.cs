using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Data;
using System.Configuration;
using System.Diagnostics;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Search;

namespace SenseNet.ContentRepository.Storage
{
    public interface IL2Cache
    {
        bool Enabled { get; set; }
        object Get(string key);
        void Set(string key, object value);
        void Clear();
    }
    internal class NullL2Cache : IL2Cache
    {
        public bool Enabled { get; set; }
        public object Get(string key) { return null; }
        public void Set(string key, object value) { return; }
        public void Clear()
        {
            // Do nothing
        }
    }

    public class StorageContext
    {
        //TODO: well configuration resolves this problem.
        internal void FixEfProviderServicesProblem()
        {
            // http://stackoverflow.com/questions/14033193/entity-framework-provider-type-could-not-be-loaded
            // The Entity Framework provider type 'System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer'
            // for the 'System.Data.SqlClient' ADO.NET provider could not be loaded. 
            // Make sure the provider assembly is available to the running application. 
            // See http://go.microsoft.com/fwlink/?LinkId=260882 for more information.

            var instance = System.Data.Entity.SqlServer.SqlProviderServices.Instance;
        }


        private static IL2Cache _l2Cache = new NullL2Cache();
        public static IL2Cache L2Cache
        {
            get { return _l2Cache; }
            set { _l2Cache = value; }
        }

    }
}
