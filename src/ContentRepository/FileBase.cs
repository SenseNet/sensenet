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
    /// <summary>
    /// Defines a base class for handling Content instances with a primary blob.
    /// </summary>
	public abstract class FileBase : GenericContent, IFile
	{
        // ================================================================================ Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBase"/> class.
        /// Do not use this constructor directly from your code.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public FileBase(Node parent) : base(parent) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FileBase"/> class.
        /// Do not use this constructor directly from your code.
	    /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public FileBase(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FileBase"/> class during the loading process.
        /// Do not use this constructor directly from your code.
	    /// </summary>
        protected FileBase(NodeToken nt) : base(nt) { }

        // ================================================================================= IFile Members

        /// <inheritdoc />
        /// <remarks>Persisted as <see cref="RepositoryDataType.Binary"/>.</remarks>>
		[RepositoryProperty("Binary", RepositoryDataType.Binary)]
		public virtual BinaryData Binary
		{
			get { return this.GetBinary("Binary"); }
			set { this.SetBinary("Binary", value); }
		}
	    /// <inheritdoc />
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
        public long FullSize //UNDONE: Make obsolete
        {
            //TODO: Create logic to calculate the sum size of all versions
            get { return -1; }
		}

        // ================================================================================= Overrides

        /// <inheritdoc />
        /// <remarks>Before saving, checks the type-consistency of executable file, if this instance is a new one.</remarks>>
	    public override void Save(NodeSaveSettings settings)
	    {
            // check new content here for speedup reasons
            if (this.IsNew)
	            AssertExecutableType(this);

	        base.Save(settings);
	    }

	    /// <summary>
	    /// Overrides the base class behavior. Triggers the validation of the executable file's type-consistency.
	    /// For example after renaming a file with .cshtml extension must be an "ExecutableFile" or its any derived one.
	    /// Do not use this method directly from your code.
	    /// </summary>
	    protected override void OnModifying(object sender, CancellableNodeEventArgs e)
	    {
            // check type in case of the name has changed
            if (e.ChangedData.Any(cd => string.Compare(cd.Name, "Name", StringComparison.InvariantCultureIgnoreCase) == 0))
                AssertExecutableType(e.SourceNode);

	        base.OnModifying(sender, e);
	    }

        // ================================================================================= Generic Property handling

	    /// <inheritdoc/>
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
	    /// <inheritdoc/>
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