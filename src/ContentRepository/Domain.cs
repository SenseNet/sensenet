using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Security.ADSync;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines an Content handler that represents a domain that is the top level element 
    /// of the identity management system in the Content Repository.
    /// </summary>
    [ContentHandler]
    public class Domain : Folder, IADSyncable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Domain"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public Domain(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Domain"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public Domain(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Domain"/> class during the loading process.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected Domain(NodeToken token) : base(token) { }

        //////////////////////////////////////// Public Properties ////////////////////////////////////////

        /// <summary>
        /// Returns true if the name of this instance is "BuiltIn".
        /// </summary>
        public bool IsBuiltInDomain => Name == IdentityManagement.BuiltInDomainName;

        // =================================================================================== IADSyncable Members

        /// <summary>
        /// Writes the given AD sync-id to the database.
        /// </summary>
        /// <param name="guid"></param>
        public void UpdateLastSync(System.Guid? guid)
        {
            if (guid.HasValue)
                this["SyncGuid"] = ((System.Guid)guid).ToString();
            this["LastSync"] = System.DateTime.UtcNow;

            this.Save();
        }
    }
}