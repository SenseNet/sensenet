using System;

namespace SenseNet.ContentRepository.Storage.Data
{
    public class ConnectionInfo
    {
        public string ConnectionName { get; set; }
        public string DataSource { get; set; }
        public string InitialCatalogName { get; set; }
        public InitialCatalog InitialCatalog { get; set; }
    }
}
