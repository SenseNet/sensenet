using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class UserProfile : Workspaces.Workspace
    {
        public UserProfile(Node parent) : this(parent, null) { }
		public UserProfile(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected UserProfile(NodeToken nt) : base(nt) { }

        private User _user;
        public User User
        {
            get { return _user ?? (_user = User.Load(this.ParentName, this.Name)); }
        }

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
