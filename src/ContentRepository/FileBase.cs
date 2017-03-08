using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using  SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository
{
	public abstract class FileBase : GenericContent, IFile
	{
        // ================================================================================ Construction

        public FileBase(Node parent) : base(parent) { }
		public FileBase(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected FileBase(NodeToken nt) : base(nt) { }

        // ================================================================================= IFile Members

		[RepositoryProperty("Binary", RepositoryDataType.Binary)]
		public virtual BinaryData Binary
		{
			get { return this.GetBinary("Binary"); }
			set { this.SetBinary("Binary", value); }
		}
        public long Size
        {
			get
			{
                // during the saving process we cannot determine the size of a binary field
                if (this.SavingState != ContentSavingState.Finalized)
                    return 0;

			    return this.GetBinary("Binary").Size;
			}
        }
        public long FullSize
        {
            //TODO: Create logic to calculate the sum size of all versions
            get { return -1; }
		}

        // ================================================================================= Overrides

	    public override void Save(NodeSaveSettings settings)
	    {
            // check new content here for speedup reasons
            if (this.IsNew)
	            AssertExecutableType(this);

	        base.Save(settings);
	    }

	    protected override void OnModifying(object sender, CancellableNodeEventArgs e)
	    {
            // check type in case of the name has changed
            if (e.ChangedData.Any(cd => string.Compare(cd.Name, "Name", StringComparison.InvariantCultureIgnoreCase) == 0))
                AssertExecutableType(e.SourceNode);

	        base.OnModifying(sender, e);
	    }

	    // ================================================================================= Generic Property handling

		public override object GetProperty(string name)
		{
			switch (name)
			{
				case "Binary":
					return this.Binary;
                case "Size":
                    return this.Size;
                case "FullSize":
                    return this.FullSize;
				default:
					return base.GetProperty(name);
			}
		}
		public override void SetProperty(string name, object value)
		{
			switch (name)
			{
				case "Binary":
					this.Binary = (BinaryData)value;
					break;
				default:
					base.SetProperty(name, value);
					break;
			}
		}

        // ================================================================================= Helper methods

	    private static void AssertExecutableType(Node node)
	    {
	        if (node == null)
	            return;

            // check if the extension interests us
            if (!RepositoryTools.IsExecutableExtension(System.IO.Path.GetExtension(node.Name)))
                return;

            // allow only executable files
            if (!RepositoryTools.IsExecutableType(node.NodeType))
                throw new InvalidContentException(string.Format(SR.GetString(SR.Exceptions.File.Error_ForbiddenExecutable_2), node.NodeType.Name, node.Name));

            // elevated mode is necessary because not every user has access to permissions
            using (new SystemAccount())
            {
                var execType = ContentType.GetByName(node.NodeType.Name);

                // check if the original user has permissions for the type
                if (!execType.Security.HasPermission(User.LoggedInUser, PermissionType.See))
                    throw new InvalidContentException(SR.GetString(SR.Exceptions.File.Error_ForbiddenExecutable)); 
            }
	    }
    }
}