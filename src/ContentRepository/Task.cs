using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class Task : GenericContent
    {
        public Task(Node parent) : this(parent, null) { }
		public Task(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected Task(NodeToken nt) : base(nt) { }

        public int RemainingDays
        {
            get 
            {
                try
                {
                    var dueDate = this.GetProperty<DateTime>("DueDate");

                    return dueDate.Year < ActiveSchema.DateTimeMinValue.Year ? 0 : Math.Abs((dueDate - DateTime.Today).Days);
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }

                return 0;
            }
        }

        public string DueText
        {
            get
            {
                try
                {
                    var dueDate = this.GetProperty<DateTime>("DueDate").Date;

                    if (dueDate < DateTime.Today) return SR.GetString("Portal", "DaysOverdue");
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }

                return SR.GetString("Portal", "DaysLeft");
            }
        }

        public string DueCssClass
        {
            get
            {
                try
                {
                    var dueDate = this.GetProperty<DateTime>("DueDate").Date;

                    if (dueDate < DateTime.Today) return "sn-deadline-overdue";
                    if (dueDate < DateTime.Today.AddDays(7)) return "sn-deadline-soon";
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex);
                }

                return "sn-deadline-later";
            }
        }

        // ================================================================================= Generic Property handling

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "RemainingDays":
                    return this.RemainingDays;
                case "DueText":
                    return this.DueText;
                case "DueCssClass":
                    return this.DueCssClass;
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}
