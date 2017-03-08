using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Packaging.Steps
{
    public class AddMembers : Step
    {
        [Annotation("Path or query that selects one existing group.")]
        public string Group { get; set; }
        [DefaultProperty]
        [Annotation("A content query or Comma or semicolon separated path list that selects one or more users or groups that will members of the group.")]
        public string Members { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if (String.IsNullOrEmpty(Group))
                throw new InvalidStepParameterException("Group cannot be null or empty");
            if (String.IsNullOrEmpty(Group))
                throw new InvalidStepParameterException("Members cannot be null or empty");

            var group = Group.StartsWith("/root", StringComparison.InvariantCultureIgnoreCase)
                ? Node.Load<Group>(Group)
                : (Group)ContentQuery.Query(Group).Nodes.FirstOrDefault();

            if (group == null)
            {
                context.Console.WriteLine("Group was not found: " + Group + ". Step execution is terminated.");
                return;
            }

            Dictionary<int, Node> members = new Dictionary<int, Node>();
            var membersSrc = Members.Trim().Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var memberSrc in membersSrc)
            {
                ParseMemberSelection(memberSrc, members);
            }

            if (members.Count == 0)
            {
                context.Console.WriteLine("Members were not found: " + membersSrc + ". Step execution is terminated.");
                return;
            }

            AddMembersToGroup(context, group, members.Values);
        }

        private void ParseMemberSelection(string src, Dictionary<int, Node> result)
        {
            src = src.Trim();

            // maybe a path
            if (src.StartsWith("/root", StringComparison.InvariantCultureIgnoreCase))
            {
                var node = Node.LoadNode(src);
                if (node != null)
                    result[node.Id] = node;
                return;
            }

            // maybe a username with domain
            var parts = src.Split('\\');
            if (parts.Length == 2)
            {
                var user = User.Load(src);
                if (user != null)
                {
                    result[user.Id] = user;
                    return;
                }
            }

            // maybe a content query
            var queryResult = ContentQuery.Query(src);
            foreach (var node in queryResult.Nodes)
            {
                result[node.Id] = node;
            }
        }

        private void AddMembersToGroup(ExecutionContext context, Group group, IEnumerable<Node> members)
        {
            var origMemberIds = group.Members.Select(m => m.Id).ToList();
            var invalid = 0;
            var valid = 0;
            foreach (var member in members)
                if (!origMemberIds.Contains(member.Id))
                    if (IsUserOrGroup(context, member, ref valid, ref invalid))
                        group.AddReference("Members", member);
            group.Save();
            context.Console.WriteLine("{0} members are added.", valid);
            if (invalid == 1)
                context.Console.WriteLine("1 content was skipped because it is not a User or a Group.");
            if (invalid > 1)
                context.Console.WriteLine("{0} content were skipped because these are not a User or a Group.", invalid);
        }

        private bool IsUserOrGroup(ExecutionContext context, Node member, ref int valid, ref int invalid)
        {
            if (member is User || member is Group)
            {
                valid++;
                return true;
            }
            invalid++;
            if (invalid <= 5)
                context.Console.WriteLine("  Skipped: " + member.Path);
            if (invalid == 6)
                context.Console.WriteLine("  There are more than 5 content that are not User or Group.");
            return false;
        }
    }
}
