using System.Collections.Generic;
using System.Web;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Portal.Virtualization;
using SenseNet.ContentRepository.Schema;
using System.Xml;
using System;
using System.Linq;
using System.Text;
using SenseNet.Search;

namespace SenseNet.Services
{
    [ContentHandler]
    public class ExportToCsvApplication : Application, IHttpHandler
    {
        public ExportToCsvApplication(Node parent) : this(parent, null) { }
        public ExportToCsvApplication(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected ExportToCsvApplication(NodeToken nt) : base(nt) { }

        private static readonly string[] YESVALUES = new[] {"1", "yes", "true"};
        protected bool ExportSystemContent
        {
            get 
            { 
                var sc = HttpContext.Current.Request.QueryString["system"];
                return !string.IsNullOrEmpty(sc) && YESVALUES.Contains(sc.Trim().ToLower());
            }
        }

        protected string ExportType
        {
            get
            {
                var et = HttpContext.Current.Request.QueryString["type"];
                return et == null ? string.Empty : et.ToLower();
            }
        }

        private Content _contentToExport;
        protected Content ExportContent
        {
            get
            {
                if (_contentToExport == null)
                {
                    _contentToExport = Content.Create(PortalContext.Current.ContextNode);
                    if (ExportSystemContent)
                        _contentToExport.ChildrenDefinition.EnableAutofilters = FilterStatus.Disabled;
                }
                return _contentToExport;
            }
        }

        // ============================================================================================= IHttpHandler Members

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Clear();
            context.Response.ContentType = "Application/x-msexcel";

            var fileName = string.Format("{0}_{1}", PortalContext.Current.ContextNode.Name, DateTime.UtcNow.ToString("yyyy-MM-dd_HHmm"));
            var b = new byte[] { 0xEF, 0xBB, 0xBF };

            context.Response.AddHeader("content-disposition", "attachment; filename=\"" + fileName + ".csv" + "\"");
            context.Response.Charset = "65001";
            context.Response.BinaryWrite(b);
            context.Response.Write(ToCsv());
            context.Response.End();
        }

        // ============================================================================================= Helper methods

        private string ToCsv()
        {
            var result = new StringBuilder();

            var header = CreateHeader();

            // writing header
            result.AppendLine("\"" + string.Join("\";\"", header) + "\"");

            try
            {
                // for all children of the current content)
                foreach (var content in ExportContent.Children)
                {
                    XmlDocument cXml;

                    try
                    {
                        cXml = new XmlDocument();
                        cXml.Load(content.GetXml());
                    }
                    catch (Exception ex)
                    {
                        SnLog.WriteException(ex);
                        continue;
                    }

                    // for all fields which the content may contain)
                    foreach (var fieldName in header)
                    {
                        try
                        {
                            if (content.Fields.ContainsKey(fieldName) && content.Fields[fieldName].HasValue())
                            {
                                XmlNode fieldNode;
                                try
                                {
                                    // content list fields need a different xpath search method
                                    fieldNode = fieldName.StartsWith("#")
                                        ? cXml.DocumentElement.SelectSingleNode("/Content/Fields/" + fieldName.Substring(1) + "[@" + Field.FIELDSUBTYPEATTRIBUTENAME + "='" + FieldSubType.ContentList + "']")
                                        : cXml.DocumentElement.SelectSingleNode("/Content/Fields/" + fieldName);
                                }
                                catch (System.Xml.XPath.XPathException)
                                {
                                    fieldNode = null;
                                }

                                var fieldValue = fieldNode != null ? fieldNode.InnerText.Replace("\"", "\"\"") : content[fieldName].ToString().Replace("\"", "\"\"");

                                // to avoid coding errors in survey rules...
                                if (fieldName == "ContentListDefinition")
                                {
                                    fieldValue = HttpUtility.HtmlDecode(fieldValue);
                                    fieldValue = fieldValue.Replace("&amp;gt;", ">");
                                    fieldValue = fieldValue.Replace("&amp;lt;", "<");
                                    fieldValue = fieldValue.Replace("&gt;", ">");
                                    fieldValue = fieldValue.Replace("&lt;", "<");
                                }
                                result.Append("\"" + fieldValue + "\"");
                            }
                        }
                        catch (Exception ex)
                        {
                            SnLog.WriteWarning(
                                $"Error during field CSV export. Content: {content.Path}. Field: {fieldName}. User: {User.Current.Username}.{Environment.NewLine}Error: {ex.ToString()}");
                            result.Append("\"\"");
                        }

                        // inserting separator or line break
                        if (header.IndexOf(fieldName) < header.Count - 1)
                        {
                            result.Append(";");
                        }
                        else
                        {
                            result.AppendLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }

            return result.ToString();
        }

        private List<string> CreateHeader()
        {
            try
            {
                switch (ExportType)
                {
                    case "visible":
                        return GetVisibleFieldNames();
                    default:
                        return GetAllFieldNames();
                }
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex);
            }

            return new List<string>();
        }

        private List<string> GetAllFieldNames()
        {
            var fieldSettings = new List<FieldSetting>();

            if (ExportContent.Children.Count() > 0)
            {
                foreach (var content in ExportContent.Children)
                {
                    AddFieldSettings(content.Fields.Values.Select(f => f.FieldSetting), fieldSettings);
                }
            }
            else
            {
                AddFieldSettings(ExportContent.Fields.Values.Select(f => f.FieldSetting), fieldSettings);

                var contentList = ExportContent.ContentHandler as ContentList;
                if (contentList != null)
                {
                    AddFieldSettings(contentList.FieldSettingContents.Cast<FieldSettingContent>().Select(fsc => fsc.FieldSetting), fieldSettings);
                }
            }

            fieldSettings.Sort(CompareFields);

            return fieldSettings.Select(fs => fs.Name).ToList();
        }

        private static readonly List<string> compulsoryFields = new List<string> { "Type", "Name" };

        private List<string> GetVisibleFieldNames()
        {
            // get leaf settings to determine visibility using the most granted mode
            var gc = ExportContent.ContentHandler as GenericContent;
            if (gc == null)
                return compulsoryFields;

            var fieldSettings = new List<FieldSetting>();
            var leafFieldSettings = gc.GetAvailableFields(false);

            foreach (var fieldSetting in leafFieldSettings)
            {
                var fs = fieldSetting;

                while (fs != null)
                {
                    if (fs.VisibleBrowse != FieldVisibility.Hide ||
                        fs.VisibleEdit != FieldVisibility.Hide ||
                        fs.VisibleNew != FieldVisibility.Hide ||
                        compulsoryFields.Contains(fs.Name))
                    {
                        // add field name if it was not added before
                        if (!string.IsNullOrEmpty(fs.Name) && !fieldSettings.Any(existingFs => existingFs.Name == fs.Name))
                        {
                            fieldSettings.Add(fs);
                        }

                        break;
                    }
                    break;
                }
            }

            fieldSettings.Sort(CompareFields);

            var fNames = fieldSettings.Select(fs => fs.Name).ToList();

            // add compulsory fields to the start of the list if they are not yet in the list
            foreach (var compulsoryField in compulsoryFields)
            {
                if (!fNames.Contains(compulsoryField))
                    fNames.Insert(0, compulsoryField);
            }

            return fNames;
        }

        private static void AddFieldSettings(IEnumerable<FieldSetting> sourceFieldSettings, ICollection<FieldSetting> fieldSettings)
        {
            foreach (var fieldSetting in sourceFieldSettings.Where(sourceFs => !fieldSettings.Any(fs => fs.Name == sourceFs.Name)))
            {
                fieldSettings.Add(fieldSetting);
            }
        }

        private static int CompareFields(FieldSetting fsX, FieldSetting fsY)
        {
            // Comparison: CTD fields come first ordered by 
            // FieldIndex, than list fields ordered by FieldIndex

            if (fsX == null)
            {
                if (fsY == null)
                    return 0;

                return -1;
            }

            if (fsY == null)
                return 1;

            if (fsX.Name.StartsWith("#"))
            {
                return fsY.Name.StartsWith("#") ? (fsX.FieldIndex ?? 0).CompareTo(fsY.FieldIndex ?? 0) : 1;
            }

            if (fsY.Name.StartsWith("#"))
                return -1;

            return (fsX.FieldIndex ?? 0).CompareTo(fsY.FieldIndex ?? 0);
        }
    }
}
