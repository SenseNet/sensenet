using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml;

// ReSharper disable once CheckNamespace
namespace SenseNet.Diagnostics
{
    public class AuditEventInfo
    {
        private readonly IAuditEvent _auditEvent;
        public int EventId => _auditEvent.EventId;
        public string Title => _auditEvent.Title;
        public string Message => _auditEvent.Message;
        public string Timestamp { get; }
        public string Severity { get; }
        public int Priority { get; }
        public string MachineName { get; }
        public string AppDomainName { get; }
        public int ProcessId { get; }
        public string ProcessName { get; }
        public int ThreadId { get; }
        public string ThreadName { get; }
        public string FormattedMessage { get; }

        public string Category { get; set; }
        public int ContentId { get; set; }
        public string ContentPath { get; set; }
        public string UserName { get; set; }

        public AuditEventInfo(IAuditEvent auditEvent, IDictionary<string, object> properties)
        {
            var formatValue = new Func<object, string>(obj =>
            {
                if (obj == null)
                    return string.Empty;
                if (obj is DateTime time)
                    return XmlConvert.ToString(time, XmlDateTimeSerializationMode.Utc);

                var s = obj as string ?? obj.ToString();
                var mustEscape = s.Any(c => c == '<' || c == '>' || c == '&' || c == '"' || c == '\'');

                return mustEscape ? HttpUtility.HtmlEncode(s) : s;
            });

            var process = Process.GetCurrentProcess();
            var thread = Thread.CurrentThread;

            _auditEvent = auditEvent;
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffff");
            Severity = "Verbose";
            Priority = -1;
            MachineName = Environment.MachineName;
            AppDomainName = Environment.UserDomainName;
            ProcessId = process.Id;
            ProcessName = process.ProcessName;
            ThreadId = thread.ManagedThreadId;
            ThreadName = thread.Name;

            // format message
            var sb = new StringBuilder();
            sb.AppendFormat("<LogEntry>").AppendLine();
            sb.AppendFormat("  <Timestamp>{0}</Timestamp>", Timestamp).AppendLine();
            sb.AppendFormat("  <Message>{0}</Message>", auditEvent.Message).AppendLine();
            sb.AppendFormat("  <Category>{0}</Category>", "Audit").AppendLine();
            sb.AppendFormat("  <Priority>{0}</Priority>", Priority).AppendLine();
            sb.AppendFormat("  <EventId>{0}</EventId>", auditEvent.EventId).AppendLine();
            sb.AppendFormat("  <Severity>{0}</Severity>", Severity).AppendLine();
            sb.AppendFormat("  <Title>{0}</Title>", auditEvent.Title).AppendLine();
            sb.AppendFormat("  <Machine>{0}</Machine>", MachineName).AppendLine();
            sb.AppendFormat("  <ApplicationDomain>{0}</ApplicationDomain>", AppDomainName).AppendLine();
            sb.AppendFormat("  <ProcessId>{0}</ProcessId>", ProcessId).AppendLine();
            sb.AppendFormat("  <ProcessName>{0}</ProcessName>", ProcessName).AppendLine();
            sb.AppendFormat("  <Win32ThreadId>{0}</Win32ThreadId>", ThreadId).AppendLine();
            sb.AppendFormat("  <ThreadName>{0}</ThreadName>", ThreadName).AppendLine();
            sb.AppendFormat("  <ExtendedProperties>").AppendLine();
            foreach (var prop in properties)
                sb.AppendFormat("    <{0}>{1}</{0}>", prop.Key, formatValue(prop.Value)).AppendLine();
            sb.AppendFormat("  </ExtendedProperties>").AppendLine();
            sb.AppendFormat("</LogEntry>").AppendLine();

            FormattedMessage = sb.ToString();

            properties.TryGetValue("Category", out var category);
            Category = (string)category;

            properties.TryGetValue("Id", out var id);
            ContentId = (int?)id ?? 0;

            properties.TryGetValue("Path", out var path);
            ContentPath = (string)path;

            properties.TryGetValue("UserName", out var userName);
            UserName = (string)userName;
        }
    }
}
