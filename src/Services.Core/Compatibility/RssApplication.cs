using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Schema;
using System.Xml;
using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.Services
{
    [ContentHandler]
    public class RssApplication : Application //UNDONE:ODATA:SERVICES: Delete (InitialTestData contains it)
    {
        public RssApplication(Node parent) : this(parent, null) { }
        public RssApplication(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected RssApplication(NodeToken nt) : base(nt) { }

        //UNDONE:ODATA RssApplication's' logic is commented out
        //public bool IsReusable
        //{
        //    get { return false; }
        //}

        //public void ProcessRequest(HttpContext context)
        //{
        //    context.Response.Clear();
        //    context.Response.ContentType = "application/xml";

        //    var writer = XmlWriter.Create(context.Response.Output, new XmlWriterSettings { Indent = true });

        //    writer.WriteProcessingInstruction("xml", @"version=""1.0"" encoding=""utf-8""");
        //    GetFeed(PortalContext.Current.ContextNode).SaveAsAtom10(writer);
        //    writer.Close();

        //    context.Response.End();
        //}

        //public static SyndicationFeed GetFeed(Node node)
        //{
        //    var lastUpdatedTime = node.ModificationDate.ToUniversalTime();
        //    return new SyndicationFeed
        //    {
        //        Title = SyndicationContent.CreatePlaintextContent(node.Name),
        //        Description = SyndicationContent.CreatePlaintextContent("{Feed desription}"),
        //        Copyright = SyndicationContent.CreatePlaintextContent("Sense/Net"),
        //        LastUpdatedTime = lastUpdatedTime, 
        //        Items = GetItems(node as IFolder)
        //    };
        //}

        //private static System.Collections.Generic.IEnumerable<SyndicationItem> GetItems(IFolder folder)
        //{
        //    if (folder == null)
        //        return new SyndicationItem[0];
        //    return from child in folder.Children select GetItem(child);
        //}
        //private static SyndicationItem GetItem(Node node)
        //{
        //    var authority = PortalContext.Current.RequestedUri.GetLeftPart(UriPartial.Authority);
        //    var sitePath = PortalContext.Current.Site?.Path ?? string.Empty + "/";
        //    var siteRelativePath = node.Path;
        //    if (node.Path.StartsWith(sitePath))
        //        siteRelativePath = siteRelativePath.Substring(sitePath.Length - 1);
        //    var browseUriString = authority + siteRelativePath;
        //    var lastUpdatedTime = node.ModificationDate.ToUniversalTime();
        //    var publishDate = lastUpdatedTime;
        //    var item = new SyndicationItem
        //    {
        //        Title = GetItemTitle(node),
        //        Summary = GetItemSummary(node),
        //        Content = GetItemContent(node, browseUriString),
        //        LastUpdatedTime = lastUpdatedTime,
        //        PublishDate = publishDate, 
        //        Id = node.Id.ToString(), 
        //    };
        //    item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(browseUriString)));
        //    return item;
        //}
        //private static TextSyndicationContent GetItemTitle(Node node)
        //{
        //    var displayName = String.IsNullOrEmpty(node.DisplayName) ? node.Name : node.DisplayName;
        //    return SyndicationContent.CreatePlaintextContent(displayName);
        //}
        //private static TextSyndicationContent GetItemSummary(Node node)
        //{
        //    var subtitleProp = (from pt in node.PropertyTypes where pt.Name == "Subtitle" && (pt.DataType == DataType.String || pt.DataType == DataType.Text) select pt).FirstOrDefault();
        //    return SyndicationContent.CreatePlaintextContent(subtitleProp == null ? node.Name : node.GetProperty<string>(subtitleProp));
        //}
        //private static SyndicationContent GetItemContent(Node node, string browseUriString)
        //{
        //    var sb = new StringBuilder();
        //    sb.Append("<html><body>");

        //    var headerProp = (from pt in node.PropertyTypes where pt.Name == "Subtitle" && (pt.DataType == DataType.String || pt.DataType == DataType.Text) select pt).FirstOrDefault();
        //    if(headerProp != null)
        //        sb.Append(node.GetProperty<string>(headerProp)).Append("<br />");

        //    WriteTypeInfo(node, sb);

        //    var folder = node as IFolder;
        //    var trashBag = node as TrashBag;

        //    // skip trash bags and non-folders
        //    if(trashBag == null && folder != null)
        //        sb.Append("<br /><a href=\"").Append(browseUriString).Append("?").Append(PortalContext.ActionParamName).Append("=RSS").Append("\">view children</a>");

        //    sb.Append("</body></html>");
        //    return SyndicationContent.CreateHtmlContent(sb.ToString());
        //}
        //private static void WriteTypeInfo(Node node, StringBuilder sb)
        //{
        //    sb.Append(node.NodeType.Name).Append(", ").Append(node.Version);
        //    var file = node as File;
        //    if (file != null)
        //        sb.Append(", ").Append(file.Binary.ContentType).Append(", ").Append(file.Binary.Size).Append(" bytes");
        //}
        //private static string GetPropertyValue(Node node, PropertyType propType)
        //{
        //    var value = node[propType];
        //    if (propType.DataType == DataType.Binary)
        //    {
        //        var bin = value as BinaryData;
        //        return string.Concat(bin.ContentType, ", ", bin.Size, " bytes");
        //    }
        //    var s = value == null ? String.Empty : value.ToString();
        //    if (s.Length < 201)
        //        return s;
        //    return s.Substring(0, 200);
        //}
    }
}
