using System;
using System.Linq;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.Formatters;
using System.Collections.Specialized;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using System.Web;
using System.Xml;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    [ConfigurationElementType(typeof(CustomFormatterData))]
    public class SnXmlFormatter : ILogFormatter
    {
        public SnXmlFormatter(NameValueCollection x)
        {
        }

        public string Format(LogEntry log)
        {
            var sb = new StringBuilder();

            sb.AppendFormat("<LogEntry>").AppendLine();
            sb.AppendFormat("  <Timestamp>{0}</Timestamp>", log.TimeStampString).AppendLine();
            sb.AppendFormat("  <Message>{0}</Message>", log.Message).AppendLine();
            sb.AppendFormat("  <Category>{0}</Category>", log.CategoriesStrings).AppendLine();
            sb.AppendFormat("  <Priority>{0}</Priority>", log.Priority).AppendLine();
            sb.AppendFormat("  <EventId>{0}</EventId>", log.EventId).AppendLine();
            sb.AppendFormat("  <Severity>{0}</Severity>", log.Severity).AppendLine();
            sb.AppendFormat("  <Title>{0}</Title>", log.Title).AppendLine();
            sb.AppendFormat("  <Machine>{0}</Machine>", log.MachineName).AppendLine();
            sb.AppendFormat("  <ApplicationDomain>{0}</ApplicationDomain>", log.AppDomainName).AppendLine();
            sb.AppendFormat("  <ProcessId>{0}</ProcessId>", log.ProcessId).AppendLine();
            sb.AppendFormat("  <ProcessName>{0}</ProcessName>", log.ProcessName).AppendLine();
            sb.AppendFormat("  <Win32ThreadId>{0}</Win32ThreadId>", log.Win32ThreadId).AppendLine();
            sb.AppendFormat("  <ThreadName>{0}</ThreadName>", log.ManagedThreadName).AppendLine();
            sb.AppendFormat("  <ExtendedProperties>").AppendLine();
            foreach (var prop in log.ExtendedProperties)
                sb.AppendFormat("    <{0}>{1}</{0}>", prop.Key, FormatValue(prop.Value)).AppendLine();
            sb.AppendFormat("  </ExtendedProperties>").AppendLine();
            sb.AppendFormat("</LogEntry>").AppendLine();

            return sb.ToString();
        }

        private string FormatValue(object obj)
        {
            if (obj == null)
                return string.Empty;
            if (obj is DateTime)
                return XmlConvert.ToString((DateTime)obj, XmlDateTimeSerializationMode.Utc);

            var s = obj as string ?? obj.ToString();
            var mustEscape = s.Any(c => c == '<' || c == '>' || c == '&' || c == '"' || c == '\'');

            return mustEscape ? HttpUtility.HtmlEncode(s) : s;
        }
    }
}

