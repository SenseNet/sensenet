using System;
using System.Threading;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// A Content handler for the root node of a <see cref="User"/>'s profile structure.
    /// </summary>
    [ContentHandler]
    public class UserProfile : Workspaces.Workspace
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserProfile"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public UserProfile(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UserProfile"/> class.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeTypeName">Name of the node type.</param>
		public UserProfile(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UserProfile"/> class during the loading process.
        /// Do not use this constructor directly in your code.
        /// </summary>
        protected UserProfile(NodeToken nt) : base(nt) { }

        private User _user;
        /// <summary>
        /// Gets the back reference of the owner <see cref="User"/> of this <see cref="UserProfile"/> instance.
        /// </summary>
        public User User
        {
            get { return _user ?? (_user = User.Load(this.ParentName, this.Name)); }
        }

        /// <inheritdoc />
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "User":
                    return this.User;
                default:
                    return base.GetProperty(name);
            }
        }

        /// <inheritdoc />
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "User":
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        [Obsolete("Use async version instead.", true)]
        public override void Save(NodeSaveSettings settings)
        {
            SaveAsync(settings, CancellationToken.None).GetAwaiter().GetResult();
        }
        public override async System.Threading.Tasks.Task SaveAsync(NodeSaveSettings settings, CancellationToken cancel)
        {
            var thisUser = this.User;
            if (thisUser != null) // skip when importing
            {
                VersionCreatedBy = thisUser;
                VersionModifiedBy = thisUser;
                CreatedBy = thisUser;
                ModifiedBy = thisUser;
                Owner = thisUser;
            }
            await base.SaveAsync(settings, cancel).ConfigureAwait(false);
        }
    }
}
