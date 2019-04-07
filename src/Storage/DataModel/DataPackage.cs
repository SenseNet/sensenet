using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.DataModel;

namespace SenseNet.Storage.DataModel
{
    public class DataPackage
    {
        /// <summary>
        /// Gets or sets the new or modified schema items.
        /// </summary>
        public RepositorySchemaData Schema { get; set; }

        /// <summary>
        /// Gets or sets the new or modified NodeHead items.
        /// </summary>
        public IEnumerable<NodeHeadData> Nodes { get; set; }

        /// <summary>
        /// Gets or sets the new or modified Version items.
        /// </summary>
        public IEnumerable<VersionData> Versions { get; set; }

        /// <summary>
        /// Gets or sets the new or modified dynamic property values by VersionId
        /// </summary>
        public IDictionary<int, DynamicData> DynamicPropertyes { get; set; }
    }
}
