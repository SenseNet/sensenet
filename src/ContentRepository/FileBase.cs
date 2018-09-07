using System;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using  SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Security;
// ReSharper disable PublicConstructorInAbstractClass

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

        /// <summary>        
        /// Gets or sets the main binary data of the file.
        /// Persisted as <see cref="RepositoryDataType.Binary"/>.
        /// </summary>
		[RepositoryProperty("Binary", RepositoryDataType.Binary)]
		public virtual BinaryData Binary
		{
			get => GetBinary("Binary");
            set => SetBinary("Binary", value);
        }
	    /// <summary>        
        /// Gets the size of the main binary data of the file.
        /// </summary>
        public long Size => SavingState != ContentSavingState.Finalized ? 0 : GetBinary("Binary").Size;

	    [Obsolete("This property and content field will be removed in the future.")]
        public long FullSize => -1;

	    // ================================================================================= Overrides

        /// <inheritdoc />
        /// <remarks>Before saving, checks the type-consistency of an executable file, if this instance is a new one.</remarks>>
	    public override void Save(NodeSaveSettings settings)
	    {
            // check new content here for speedup reasons
	        if (IsNew)
	        {
	            AssertExecutableType(this);
	            SetBinaryData(this);
	        }
	        base.Save(settings);
	    }

	    /// <summary>
	    /// Overrides the base class behavior. Triggers the validation of the executable file's type-consistency.
	    /// For example after renaming a file to have a .cshtml extension, it must be an "ExecutableFile" or a derived type.
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
					return Binary;
                case "Size":
                    return Size;
                case "FullSize":
#pragma warning disable 618
                    // Need to use even if deprecated because it is protected.
                    return FullSize;
#pragma warning restore 618
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
					Binary = (BinaryData)value;
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

	    private static void SetBinaryData(FileBase file)
	    {
	        var binaryData = file.Binary;

	        var mimeTypeOriginal = binaryData.ContentType;

	        if (string.IsNullOrEmpty(binaryData.FileName))
	            binaryData.FileName = file.Name;

	        if (string.IsNullOrEmpty(mimeTypeOriginal))
	        {
	            var extension = System.IO.Path.GetExtension(file.Name);
	            var mimeType = MimeTable.GetMimeType(extension);
	            binaryData.ContentType = mimeType;
	        }
	        else
	        {
	            binaryData.ContentType = mimeTypeOriginal;
	        }
        }
    }
}