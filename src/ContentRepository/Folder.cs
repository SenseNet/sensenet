using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines constants for enabling preview generation.
    /// </summary>
    public enum PreviewEnabled
    {
        /// <summary>Preview generation depends on the parent content.</summary>
        Inherited,
        /// <summary>Preview generation is disabled.</summary>
        No,
        /// <summary>Preview generation is enabled.</summary>
        Yes
    }

    /// <summary>
    /// A Content handler class for representing a container for child Content items.
    /// </summary>
	[ContentHandler]
    public class Folder : GenericContent, IFolder
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="Folder"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public Folder(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Folder"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
        public Folder(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Folder"/> class in the loading procedure.
        /// Do not use this constructor directly from your code.
        /// </summary>
        protected Folder(NodeToken nt) : base(nt) { }

	    /// <inheritdoc select="summary" />
        /// <remarks>This value cannot be modified in case of simple Folders,
        /// they inherit this list from their parent.</remarks>
        public override IEnumerable<ContentType> AllowedChildTypes
        {
            get { return base.AllowedChildTypes; }
            set
            {
                if (this.NodeType.Name == "Folder")
                    return;
                base.AllowedChildTypes = value;
            }
        }

	    /// <inheritdoc />
        public virtual IEnumerable<Node> Children
        {
            get { return this.GetChildren(); }
        }
	    /// <inheritdoc />
        public virtual int ChildCount
        {
            get { return this.GetChildCount();}
        }

        public override bool IsPreviewEnabled
        {
            get
            {
                //UNDONE:xxxxPreview: ? Check ContentType 
                switch (PreviewEnabled)
                {
                    case ContentRepository.PreviewEnabled.Inherited:
                        using (new SystemAccount())
                            return Parent?.IsPreviewEnabled ?? false;
                    case ContentRepository.PreviewEnabled.No:
                        return false;
                    case ContentRepository.PreviewEnabled.Yes:
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that stores this content's state: preview is enabled, disabled, or depends on the parent.
        /// </summary>
        [RepositoryProperty(nameof(PreviewEnabled), RepositoryDataType.String)]
        public PreviewEnabled PreviewEnabled
        {
            get
            {
                var result = PreviewEnabled.Inherited;
                var enumVal = base.GetProperty<string>(nameof(PreviewEnabled));
                if (string.IsNullOrEmpty(enumVal))
                    return result;
                Enum.TryParse(enumVal, false, out result);
                return result;
            }
            set
            {
                this[nameof(PreviewEnabled)] = Enum.GetName(typeof(PreviewEnabled), value);
            }
        }

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case GenericContent.ALLOWEDCHILDTYPES:
                    return this.AllowedChildTypes;
                case nameof(PreviewEnabled):
                    return this.PreviewEnabled;
                default:
                    return base.GetProperty(name);
            }
        }
	    /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case GenericContent.ALLOWEDCHILDTYPES:
                    this.AllowedChildTypes = (IEnumerable<ContentType>)value;
                    break;
                case nameof(PreviewEnabled):
                    this.PreviewEnabled = (PreviewEnabled) value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        protected override void OnDeletingPhysically(object sender, CancellableNodeEventArgs e)
        {
            base.OnDeletingPhysically(sender, e);
        
            if (!Path.StartsWith(RepositoryStructure.ImsFolderPath + RepositoryPath.PathSeparator))
                return;

            // If we are deleting a container under the security folder, we have to check whether
            // we would delete users from protected groups. If any of the protected groups would
            // become empty, this will throw an exception.
            using (new SystemAccount())
            {
                var userInSubtree = ContentQuery.QueryAsync(SafeQueries.UsersInSubtree, QuerySettings.AdminSettings,
                    CancellationToken.None, Path).ConfigureAwait(false).GetAwaiter().GetResult().Identifiers.ToArray();

                User.AssertEnabledParentGroupMembers(userInSubtree);
            }
        }
    }
}