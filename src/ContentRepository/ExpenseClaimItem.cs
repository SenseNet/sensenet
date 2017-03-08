using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class ExpenseClaimItem : GenericContent
    {
        // ============================================================== Constructors

        protected ExpenseClaimItem(Node parent) : base(parent) {}
        public ExpenseClaimItem(Node parent, string nodeTypeName) : base(parent, nodeTypeName) {}
        protected ExpenseClaimItem(NodeToken nt) : base(nt) {}

        // ============================================================== Overrides

        protected override void OnDeletedPhysically(object sender, Storage.Events.NodeEventArgs e)
        {
            base.OnDeletedPhysically(sender, e);
            RefreshExpenseClaim();
        }

        protected override void OnModified(object sender, Storage.Events.NodeEventArgs e)
        {
            base.OnModified(sender, e);
            RefreshExpenseClaim();
        }

        protected override void OnCreated(object sender, Storage.Events.NodeEventArgs e)
        {
            base.OnCreated(sender, e);
            RefreshExpenseClaim();
        }

        // ============================================================== Helper methods

        protected void RefreshExpenseClaim()
        {
            var ec = this.Parent as ExpenseClaim;
            if (ec == null)
                return;

            if (ec.Version.Status != VersionStatus.Approved &&
                ec.Version.Status != VersionStatus.Pending)
                return;

            ec.Save();
        }
    }
}
