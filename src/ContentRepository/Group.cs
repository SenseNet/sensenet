using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.ContentRepository.Security.ADSync;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Events;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class Group : GenericContent, IGroup, IADSyncable
    {
        public Group(Node parent) : this(parent, null) { }
        public Group(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Group(NodeToken token) : base(token) { }

        //////////////////////////////////////// Public Properties ////////////////////////////////////////

        public const string MEMBERS = "Members";
        [RepositoryProperty(MEMBERS, RepositoryDataType.Reference)]
        public IEnumerable<Node> Members
        {
            get { return this.GetReferences(MEMBERS); }
            set { this.SetReferences(MEMBERS, value); }
        }
        public List<Group> Groups
        {
            get
            {
                var groups = new List<Group>();
                foreach (Node node in Members)
                    if (node is Group)
                        groups.Add((Group)node);
                return groups;
            }
        }
        public List<User> Users
        {
            get
            {
                var users = new List<User>();
                foreach (Node node in Members)
                    if (node is User)
                        users.Add((User)node);
                return users;
            }
        }

        private Domain _domain;
        public Domain Domain
        {
            get { return _domain ?? (_domain = Node.GetAncestorOfType<Domain>(this)); }
        }

        //////////////////////////////////////// Private Members ////////////////////////////////////////
        private bool _syncObject = true;


        //////////////////////////////////////// Static Members ////////////////////////////////////////

        public static Group Administrators
        {
            get { return (Group)Node.LoadNode(Identifiers.AdministratorsGroupId); }
        }
        public static Group Everyone
        {
            get { return (Group)Node.LoadNode(Identifiers.EveryoneGroupId); }
        }
        public static Group RegisteredUsers
        {
            get { return Node.Load<Group>("/Root/IMS/BuiltIn/Portal/RegisteredUsers"); }
        }
        public static Group Operators
        {
            get { return (Group)Node.LoadNode(Identifiers.OperatorsGroupPath); }
        }
        public static Group Owners
        {
            get { return (Group)Node.LoadNode(Identifiers.OwnersGroupId); }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case MEMBERS:
                    return this.Members;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case MEMBERS:
                    this.Members = (IEnumerable<Node>)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        public List<IUser> GetMemberUsers()
        {
            List<IUser> memberUsers = new List<IUser>();
            foreach (Node node in Members)
            {
                IUser user = node as IUser;
                if (user != null) memberUsers.Add(user);
            }
            return memberUsers;
        }

        public List<IGroup> GetMemberGroups()
        {
            List<IGroup> memberGroups = new List<IGroup>();
            foreach (Node node in Members)
            {
                IGroup group = node as IGroup;
                if (group != null) memberGroups.Add(group);
            }
            return memberGroups;
        }

        public List<IUser> GetAllMemberUsers()
        {
            var memberUsers = new Dictionary<int, IUser>();

            GetMembers(memberUsers, this);

            return memberUsers.Values.ToList();
        }

        public List<IGroup> GetAllMemberGroups()
        {
            var memberGroups = new Dictionary<int, IGroup>();

            GetMembers(memberGroups, this);

            return memberGroups.Values.ToList();
        }

        private void GetMembers(Dictionary<int, IUser> memberUsers, IGroup group)
        {
            foreach (var member in group.Members)
            {
                var user = member as IUser;
                if (user != null)
                {
                    memberUsers[user.Id] = user;
                }
                else
                {
                    var childGroup = member as IGroup;
                    // in order to avoid circles
                    if (childGroup != null && this.Id != childGroup.Id)
                        GetMembers(memberUsers, childGroup);
                }
            }
        }

        private void GetMembers(Dictionary<int, IGroup> memberGroups, IGroup group)
        {
            foreach (var member in group.Members)
            {
                var childGroup = member as IGroup;
                if (childGroup != null && !memberGroups.ContainsKey(childGroup.Id) && this.Id != childGroup.Id)
                {
                    memberGroups[childGroup.Id] = childGroup;               
                    GetMembers(memberGroups, childGroup);
                }
            }
        }

        [Obsolete("Use IsInGroup instead.", false)]
        public bool IsInRole(int securityGroupId)
        {
            return IsInGroup(securityGroupId);
        }
        public bool IsInGroup(int securityGroupId)
        {
            return SecurityHandler.IsInGroup(this.Id, securityGroupId);
        }

        public void AddMember(IGroup group)
        {
            AssertSpecialGroup(SR.Exceptions.Group.CannotAddMembersToASpecialGroup);

            if (group == null)
                throw new ArgumentNullException("group");

            var groupNode = group as Node;
            if (groupNode == null)
                throw new ArgumentOutOfRangeException("group", "The given value is not a Node.");

            this.AddReference(MEMBERS, groupNode);

            Save();
        }

        public void AddMember(IUser user)
        {
            AssertSpecialGroup(SR.Exceptions.Group.CannotAddMembersToASpecialGroup);

            if (user == null)
                throw new ArgumentNullException("user");

            var userNode = user as Node;
            if (userNode == null)
                throw new ArgumentOutOfRangeException("user", "The given value is not a Node.");

            this.AddReference(MEMBERS, userNode);

            Save();
        }

        public void RemoveMember(IGroup group)
        {
            AssertSpecialGroup(SR.Exceptions.Group.CannotRemoveMembersFromASpecialGroup);

            if (group == null)
                throw new ArgumentNullException("group");

            var groupNode = group as Node;
            if (groupNode == null)
                throw new ArgumentOutOfRangeException("group", "The given value is not a Node.");

            this.RemoveReference(MEMBERS, groupNode);

            Save();
        }

        public void RemoveMember(IUser user)
        {
            AssertSpecialGroup(SR.Exceptions.Group.CannotRemoveMembersFromASpecialGroup);

            if (user == null)
                throw new ArgumentNullException("user");

            var userNode = user as Node;
            if (userNode == null)
                throw new ArgumentOutOfRangeException("user", "The given value is not a Node.");

            this.RemoveReference(MEMBERS, userNode);

            Save();
        }

        private void AssertSpecialGroup(string msg)
        {
            if (this.Id != Identifiers.EveryoneGroupId &&
                this.Id != Identifiers.OwnersGroupId)
                return;
            throw new InvalidOperationException(String.Format(msg, this.Name));
        }
        
        public override void Save(NodeSaveSettings settings)
        {
            AssertValidMembers();

            var originalId = this.Id;

            base.Save(settings);

            // AD Sync
            if (_syncObject)
            {
                ADFolder.SynchADContainer(this, originalId);
            }
            // default: object should be synced. if it was not synced now (sync properties updated only) next time it should be.
            _syncObject = true;
        }

        public override void ForceDelete()
        {
            base.ForceDelete();

            // AD Sync
            var ADProvider = DirectoryProvider.Current;
            if (ADProvider != null)
            {
                ADProvider.DeleteADObject(this);
            }
        }

        public override bool IsTrashable
        {
            get
            {
                return false;
            }
        }

        public override void MoveTo(Node target)
        {
            base.MoveTo(target);

            // AD Sync
            var ADProvider = DirectoryProvider.Current;
            if (ADProvider != null)
            {
                ADProvider.UpdateADContainer(this, RepositoryPath.Combine(target.Path, this.Name));
            }
        }

        protected void AssertValidMembers()
        {
            // only check existing groups
            if (this.Id == 0)
                return;

            if (Identifiers.SpecialGroupPaths.Contains(this.Path))
            {
                if (this.Members.Any())
                    throw new InvalidOperationException(string.Format("The {0} group is a special system group, members cannot be added to it.", this.DisplayName));
            }

            if (this.Members.OfType<Group>().Any(@group => @group.Id == this.Id))
            {
                    throw new InvalidOperationException(string.Format("Group cannot contain itself as a member. Please remove {0} from the Members list.", this.DisplayName));
            }
        }

        // =================================================================================== OData API

        [ODataAction]
        public static object AddMembers(Content content, int[] contentIds)
        {
            RepositoryTools.AssertArgumentNull(content, "content");
            RepositoryTools.AssertArgumentNull(contentIds, "contentIds");

            var group = content.ContentHandler as Group;
            if (group == null)
                throw new InvalidOperationException(SR.Exceptions.Group.NotAGroup);

            group.AssertSpecialGroup(SR.Exceptions.Group.CannotAddMembersToASpecialGroup);

            // add the provided reference nodes
            group.AddReferences<Node>(MEMBERS, Node.LoadNodes(contentIds));
            group.Save();

            return null;
        }

        [ODataAction]
        public static object RemoveMembers(Content content, int[] contentIds)
        {
            RepositoryTools.AssertArgumentNull(content, "content");
            RepositoryTools.AssertArgumentNull(contentIds, "contentIds");

            var group = content.ContentHandler as Group;
            if (group == null)
                throw new InvalidOperationException(SR.Exceptions.Group.NotAGroup);

            group.AssertSpecialGroup(SR.Exceptions.Group.CannotRemoveMembersFromASpecialGroup);

            // remove all the provided referenced nodes
            Node.LoadNodes(contentIds).ForEach(refNode => group.RemoveReference(MEMBERS, refNode));

            group.Save();

            return null;
        }

        // =================================================================================== Events

        protected override void OnMoving(object sender, CancellableNodeOperationEventArgs e)
        {
            // AD Sync check
            var ADProvider = DirectoryProvider.Current;
            if (ADProvider != null)
            {
                var targetNodePath = RepositoryPath.Combine(e.TargetNode.Path, this.Name);
                var allowMove = ADProvider.AllowMoveADObject(this, targetNodePath);
                if (!allowMove)
                {
                    e.CancelMessage = "Moving of synced nodes is only allowed within AD server bounds!";
                    e.Cancel = true;
                }
            }

            base.OnMoving(sender, e);
        }

        protected override void OnCreated(object sender, NodeEventArgs e)
        {
            base.OnCreated(sender, e);

            // insert this group to the security graph
            using (new SystemAccount())
            {
                var parent = GroupMembershipObserver.GetFirstOrgUnitParent(e.SourceNode);
                if (parent != null)
                    SecurityHandler.AddGroupsToGroup(parent.Id, new[] { e.SourceNode.Id });
            }

            var usersToAdd = GetMemberUsers().Select(u => u.Id).ToArray();
            var groupsToAdd = GetMemberGroups().Select(g => g.Id).ToArray();

            if (usersToAdd.Length > 0 || groupsToAdd.Length > 0)
                SecurityHandler.AddMembers(this.Id, usersToAdd, groupsToAdd);
        }

        protected override void OnModified(object sender, NodeEventArgs e)
        {
            base.OnModified(sender, e);

            UpdateMembership(e);
        }

        protected void UpdateMembership(NodeEventArgs e)
        {
            if (e.ChangedData == null)
                return;

            // load and parse member list
            var membersData = e.ChangedData.FirstOrDefault(cd => string.Compare(cd.Name, MEMBERS, StringComparison.InvariantCulture) == 0);
            if (membersData == null)
                return;

            var oldMembers = (membersData.Original as string ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(m => Convert.ToInt32(m)).ToArray();
            var newMembers = (membersData.Value as string ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(m => Convert.ToInt32(m)).ToArray();

            var addedIdentities = newMembers.Except(oldMembers);
            var removedIdentities = oldMembers.Except(newMembers);

            // I chose collecting arrays over LINQ here because this way we enumerate and load nodeheads only once
            var ntUser = ActiveSchema.NodeTypes["User"];
            var ntGroup = ActiveSchema.NodeTypes["Group"];
            var usersToAdd = new List<int>();
            var usersToRemove = new List<int>();
            var groupsToAdd = new List<int>();
            var groupsToRemove = new List<int>();

            // collect users and groups to add
            foreach (var nodeHead in addedIdentities.Select(NodeHead.Get).Where(nh => nh != null))
            {
                if (nodeHead.GetNodeType().IsInstaceOfOrDerivedFrom(ntUser))
                    usersToAdd.Add(nodeHead.Id);
                else if (nodeHead.GetNodeType().IsInstaceOfOrDerivedFrom(ntGroup))
                    groupsToAdd.Add(nodeHead.Id);
            }

            // collect users and groups to remove
            foreach (var nodeHead in removedIdentities.Select(NodeHead.Get).Where(nh => nh != null))
            {
                if (nodeHead.GetNodeType().IsInstaceOfOrDerivedFrom(ntUser))
                    usersToRemove.Add(nodeHead.Id);
                else if (nodeHead.GetNodeType().IsInstaceOfOrDerivedFrom(ntGroup))
                    groupsToRemove.Add(nodeHead.Id);
            }

            if (usersToRemove.Count > 0 || groupsToRemove.Count > 0)
                SecurityHandler.RemoveMembers(this.Id, usersToRemove, groupsToRemove);
            if (usersToAdd.Count > 0 || groupsToAdd.Count > 0)
                SecurityHandler.AddMembers(this.Id, usersToAdd, groupsToAdd);
        }

        // =================================================================================== IADSyncable Members

        public void UpdateLastSync(Guid? guid)
        {
            if (guid.HasValue)
                this["SyncGuid"] = ((Guid)guid).ToString();
            this["LastSync"] = DateTime.UtcNow;

            // update object without syncing to AD
            _syncObject = false;

            this.Save();
        }
    }
}
