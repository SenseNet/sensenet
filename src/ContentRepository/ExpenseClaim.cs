using System;
using System.Linq;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search;
using SenseNet.Search;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class ExpenseClaim : Folder
    {
        public ExpenseClaim(Node parent) : this(parent, null) { }
        public ExpenseClaim(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ExpenseClaim(NodeToken nt) : base(nt) { }

        [Obsolete("Use GetApprover(budgetLimit, CEO) instead.", true)]
        public User GetApprover()
        {
            return null;
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
                if (!SearchManager.ContentQueryIsAllowed)
                    return 0;

                QueryResult cq = ContentQuery.Query(SafeQueries.InTreeAndTypeIs, null, this.Path, "expenseclaimitem");
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