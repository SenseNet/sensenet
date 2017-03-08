using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using  SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage.Search;
using System.Web;
using System.Linq;

namespace SenseNet.ContentRepository
{
    [ContentHandler]
    public class ContentRotator : SmartFolder, ICustomSingleNode
    {
        public class validityOrderComparer : IComparer<Node>
        {
            public int Compare(Node x, Node y)
            {
                DateTime xValidFrom;
                DateTime yValidFrom;

                if (!GetNodeDateProperty(x, "ValidFrom", out xValidFrom))
                    return 0;

                if (!GetNodeDateProperty(y, "ValidFrom", out yValidFrom))
                    return 0;

                if (xValidFrom < yValidFrom)
                    return -1;
                if (xValidFrom > yValidFrom)
                    return 1;
                return 0;
            }
        }

        // ===================================================================================== Construction

        public ContentRotator(Node parent) : this(parent, null) { }
        public ContentRotator(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ContentRotator(NodeToken nt) : base(nt) { }

        // ===================================================================================== Properties

        public DateTime CurrentDate
        {
            get
            {
                string dateString = HttpContext.Current.Request.QueryString["CurrentDate"];
                return string.IsNullOrEmpty(dateString) ? DateTime.UtcNow : DateTime.Parse(dateString);
            }
        }

		[RepositoryProperty("SelectionMode", RepositoryDataType.String)]
		public string SelectionMode
		{
			get { return this.GetProperty<string>("SelectionMode"); }
			set { this["SelectionMode"] = value; }
		}

		[RepositoryProperty("OrderingMode", RepositoryDataType.String)]
		public string OrderingMode
		{
			get { return this.GetProperty<string>("OrderingMode"); }
			set { this["OrderingMode"] = value; }
		}

        // ===================================================================================== Method

        public Node SelectRandomItem(IEnumerable<Node> children)
        {
            int count = children.Count<Node>();
            if (count <= 1)
                return children.FirstOrDefault<Node>();

            Random rand = new Random();
            int randomSelected = rand.Next(count);

            return children.ElementAtOrDefault<Node>(randomSelected);
        }
        public Node SelectFirstItem(IEnumerable<Node> children)
        {
            return children.FirstOrDefault<Node>();
        }

        public static bool GetNodeDateProperty(Node node, string propertyName, out DateTime date)
        {
			date = DateTime.MinValue;
			if (!node.HasProperty(propertyName))
				return false;
			date = node.GetProperty<DateTime>(propertyName);
			return true;
        }

        public IEnumerable<Node> GetValidItems(IEnumerable<Node> items)
        {
            foreach (Node node in items)
            {
                DateTime validFrom;
                DateTime validTill;

                // not published
                if (node.Version.Status != VersionStatus.Approved)
                    continue;

                // not valid by date
                if (!GetNodeDateProperty(node, "ValidFrom", out validFrom))
                    continue;

                if (!GetNodeDateProperty(node, "ValidTill", out validTill))
                    continue;

                if ((validFrom < CurrentDate) && (validTill > CurrentDate))
                    yield return node;
            }
        }

        public IEnumerable<Node> GetPublishedItems(IEnumerable<Node> items)
        {
            foreach (Node node in items)
            {
                // not published
                if (node.Version.Status != VersionStatus.Approved)
                    continue;

                yield return node;
            }
        }


        public IEnumerable<Node> SortedItems
        {
            get
            {
                IEnumerable<Node> items = this.GetChildren();

                switch (OrderingMode)
                {
                    case "ValidityOrder":
                        var validItems = GetValidItems(items);
                        var sortedItems = validItems.OrderBy(a => a, new validityOrderComparer());
                        return sortedItems;
                    default:
                        return GetPublishedItems(items);
                }
            }
        }

        public Node SelectedItem()
        {
            // selection mode
            switch (SelectionMode)
            {
                case "Random":
                    return SelectRandomItem(SortedItems);
                case "First":
                    return SelectFirstItem(SortedItems);
                default:
                    return SelectRandomItem(SortedItems);
            }
        }

        public override object GetProperty(string name)
        {
            switch (name)
            {
                case "SelectionMode":
                    return this.SelectionMode;
                case "OrderingMode":
                    return this.OrderingMode;
                case "SelectedItem":
                    Node selectedItem = this.SelectedItem();
                    if (selectedItem == null)
                        return string.Empty;
                    return string.Concat(selectedItem.Name, "<br/>", selectedItem.Path);
                default:
                    return base.GetProperty(name);
            }
        }
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case "SelectionMode":
                    this.SelectionMode = (string)value;
                    break;
                case "OrderingMode":
                    this.OrderingMode = (string)value;
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }
    }
}