using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines a content handler for root node of an <see cref="User"/>'s profile structure.
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
        /// Gets back reference of the owner <see cref="User"/> of this <see cref="UserProfile"/> instance.
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

        /// <inheritdoc />
        public override void Save(NodeSaveSettings settings)
        {
            var thisUser = this.User;
            if (thisUser != null) // skip when importing
            {
                this.VersionCreatedBy = thisUser;
                this.VersionModifiedBy = thisUser;
                this.CreatedBy = thisUser;
                this.ModifiedBy = thisUser;
                this.Owner = thisUser;
            }
            base.Save(settings);
        }
    }
}
