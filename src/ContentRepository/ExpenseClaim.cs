using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.JScript;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Versioning;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class ExpenseClaim : Folder
    {
        public ExpenseClaim(Node parent) : this(parent, null) { }
        public ExpenseClaim(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ExpenseClaim(NodeToken nt) : base(nt) { }

        private Node GetAssociatedWorkflow()
        {
            if (HttpContext.Current != null && HttpContext.Current.Request.QueryString.AllKeys.Contains("ContentTypeName"))
            {
                var contentTypeName = HttpContext.Current.Request["ContentTypeName"];

                return string.IsNullOrEmpty(contentTypeName)
                           ? null
                           : Node.LoadNode(contentTypeName);
            }

            return null;
        }

        public User GetApprover()
        {
            var workflow = this.GetAssociatedWorkflow();
            
            return this.GetApprover(workflow.GetProperty<int>("BudgetLimit"), workflow.GetReference<User>("CEO"));
        }

        public User GetApprover(int budgetLimit, User CEO)
        {
            var manager = this.CreatedBy.GetReference<User>("Manager");

            if (this.Sum > budgetLimit || manager == null)
                return CEO;
            else
                return manager;
        }

        public int Sum
        {
            get
            {
                if (!StorageContext.Search.ContentQueryIsAllowed)
                    return 0;

                QueryResult cq = ContentQuery_NEW.Query(SafeQueries.InTreeAndTypeIs, null, this.Path, "expenseclaimitem");
                return Convert.ToInt32(cq.Nodes.Sum(elem => (decimal)elem["Amount"]));
            }
        }
        
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "Sum":
                    return this.Sum;
                default:
                    return base.GetProperty(name);
            }
        }

        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "BudgetLimit":
                    break;
                case "Sum":
                    break;
                case "Approver":
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}