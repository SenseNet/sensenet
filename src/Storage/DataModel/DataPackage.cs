using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.DataModel
{
    public class DataPackage
    {
        /// <summary>
        /// Gets or sets the relative filesystem path of the binary streams in the DynamicProperties.BinaryProperties
        /// If the streams are not in the filesystem, this value need to be null.
        /// </summary>
        public string RootPath { get; set; }

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
        public IEnumerable<DynamicPropertyData> DynamicProperties { get; set; }
    }
}
