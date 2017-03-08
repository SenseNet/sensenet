using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using SenseNet.ContentRepository.Storage.Security;
using System.Configuration;
using SenseNet.Configuration;

namespace SenseNet.Diagnostics
{
    public class Logger
    {
        #region Obsolete
        [Obsolete("Do not use anymore.")]
        public static class EventId
        {
            public const int NotDefined = 1;
        }

        [Obsolete("Do not use anymore.")]
        public static class Category
        {
            public static readonly string Messaging = "Messaging";
        }

        [Obsolete("Do not use anymore.")]
        public static int DefaultPriority = -1;
        [Obsolete("Do not use anymore.")]
        public static TraceEventType DefaultSeverity = TraceEventType.Information;
        [Obsolete("Do not use anymore.")]
        public static string DefaultTitle = string.Empty;

        [Obsolete("Use one of the SnLog.WriteXXXX methods instead.")]
        public static void Write(object message)
        {
            WritePrivate(message);
        }
        [Obsolete("Use one of the SnLog.WriteXXXX methods instead.")]
        public static void Write(object message, string category)
        {
            ICollection<string> categories = new string[] { category };

            WritePrivate(message, categories);
        }
        [Obsolete("Use one of the SnLog.WriteXXXX methods instead.")]
        public static void Write(object message, ICollection<string> categories)
        {
            WritePrivate(message, categories);
        }
        [Obsolete("Use one of the SnLog.WriteXXXX methods instead.")]
        public static void Write(object message, ICollection<string> categories, TraceEventType severity)
        {
            WritePrivate(message, categories, severity: severity);
        }
        [Obsolete("Use one of the SnLog.WriteXXXX methods instead.")]
        public static void Write(object message, IEnumerable<string> categories, int priority, int eventId, TraceEventType severity, string title, IDictionary<string, object> properties)
        {
            WritePrivate(message, categories, priority, eventId, severity, title, properties);
        }


        [Obsolete("Use SnLog.WriteCritical instead.")]
        public static void WriteCritical(
            int eventId,
            object message,
            IEnumerable<string> categories = null,
            int priority = -1,
            string title = null,
            IDictionary<string, object> properties = null)
        {
            SnLog.WriteError(message, eventId, categories, priority, title, properties);
        }
        [Obsolete("Use SnLog.WriteCritical instead.")]
        public static void WriteCritical<T>(int eventId, object message, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            SnLog.WriteError(message, eventId, properties: getPropertiesCallback?.Invoke(callbackArg));
        }
        [Obsolete("Use SnLog.WriteCritical instead.")]
        public static void WriteCritical<T>(int eventId, object message, IEnumerable<string> categories, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            SnLog.WriteError(message, eventId, categories, properties: getPropertiesCallback?.Invoke(callbackArg));
        }


        [Obsolete("Use SnLog.WriteError instead.")]
        public static void WriteError(
            int eventId,
            object message,
            IEnumerable<string> categories = null,
            int priority = -1,
            string title = null,
            IDictionary<string, object> properties = null)
        {
            WritePrivate(message, categories, priority, eventId, TraceEventType.Error, title, properties);
        }
        [Obsolete("Use SnLog.WriteError instead.")]
        public static void WriteError<T>(object message, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            SnLog.WriteError(message, properties: getPropertiesCallback?.Invoke(callbackArg));
        }
        [Obsolete("Use SnLog.WriteError instead.")]
        public static void WriteError<T>(object message, IEnumerable<string> categories, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            SnLog.WriteError(message, categories: categories, properties: getPropertiesCallback?.Invoke(callbackArg));
        }


        [Obsolete("Use SnLog.WriteInformation instead.")]
        public static void WriteInformation(
            int eventId,
            object message,
            IEnumerable<string> categories = null,
            int priority = -1,
            string title = null,
            IDictionary<string, object> properties = null)
        {
            SnLog.WriteInformation(message, eventId, categories, priority, title, properties);
        }
        [Obsolete("Use SnLog.WriteInformation instead.")]
        public static void WriteInformation<T>(object message, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            SnLog.WriteInformation(message, properties: getPropertiesCallback?.Invoke(callbackArg));
        }
        [Obsolete("Use SnLog.WriteInformation instead.")]
        public static void WriteInformation<T>(object message, IEnumerable<string> categories, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            SnLog.WriteInformation(message, categories: categories, properties: getPropertiesCallback?.Invoke(callbackArg));
        }


        [Obsolete("Use SnLog.WriteWarning instead.")]
        public static void WriteWarning(
            int eventId,
            object message,
            IEnumerable<string> categories = null,
            int priority = -1,
            string title = null,
            IDictionary<string, object> properties = null)
        {
            WritePrivate(message, categories, priority, eventId, TraceEventType.Warning, title, properties);
        }
        [Obsolete("Use SnLog.WriteWarning instead.")]
        public static void WriteWarning<T>(object message, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            SnLog.WriteWarning(message, properties: getPropertiesCallback?.Invoke(callbackArg));
        }
        [Obsolete("Use SnLog.WriteWarning instead.")]
        public static void WriteWarning<T>(object message, IEnumerable<string> categories, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            SnLog.WriteWarning(message, categories: categories, properties: getPropertiesCallback?.Invoke(callbackArg));
        }


        [Obsolete("Use SnTrace for verbose logging.")]
        public static void WriteVerbose(object message)
        {
            SnTrace.Write(message.ToString());
        }
        [Obsolete("Use SnTrace for verbose logging.")]
        public static void WriteVerbose(object message, IEnumerable<string> categories)
        {
            var p = new Dictionary<string, object>();
            if (categories != null)
                p.Add("categories", string.Join(",", categories));
            var props = ". {" + string.Join(", ", p.Select(x => x.Key + ": " + x.Value)) + "}";
            SnTrace.Write($"{message}. {props}");
        }
        [Obsolete("Use SnTrace for verbose logging.")]
        public static void WriteVerbose(object message, IDictionary<string, object> properties)
        {
            var p = properties == null ? new Dictionary<string, object>() : new Dictionary<string, object>(properties);
            var props = ". {" + string.Join(", ", p.Select(x => x.Key + ": " + x.Value)) + "}";
            SnTrace.Write($"{message}. {props}");
        }
        [Obsolete("Use SnTrace for verbose logging.")]
        public static void WriteVerbose(object message, IEnumerable<string> categories, IDictionary<string, object> properties)
        {
            var p = new Dictionary<string, object>();
            if (categories != null)
                p.Add("categories", string.Join(",", categories));
            var props = ". {" + string.Join(", ", p.Select(x => x.Key + ": " + x.Value)) + "}";
            SnTrace.Write($"{message}. {props}");
        }
        [Obsolete("Use SnTrace for verbose logging.")]
        public static void WriteVerbose(object message, IEnumerable<string> categories, int priority, int eventId, string title, IDictionary<string, object> properties)
        {
            var p = properties == null ? new Dictionary<string, object>() : new Dictionary<string, object>(properties);
            if (categories != null)
                p.Add("categories", string.Join(",", categories));
            if(title != null)
                p.Add("title", title);
            p.Add("priority", priority);
            p.Add("eventId", eventId);

            var props = ". {" + string.Join(", ", p.Select(x => x.Key + ": " + x.Value)) + "}";

            SnTrace.Write($"{message}. {props}");
        }
        [Obsolete("Use SnTrace for verbose logging.")]
        public static void WriteVerbose<T>(object message, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            var d = getPropertiesCallback?.Invoke(callbackArg);
            var p = d == null ? new Dictionary<string, object>() : new Dictionary<string, object>(d);
            var props = ". {" + string.Join(", ", p.Select(x => x.Key + ": " + x.Value)) + "}";

            SnTrace.Write($"{message}. {props}");
        }
        [Obsolete("Use SnTrace for verbose logging.")]
        public static void WriteVerbose<T>(object message, IEnumerable<string> categories, Func<T, IDictionary<string, object>> getPropertiesCallback, T callbackArg)
        {
            var d = getPropertiesCallback?.Invoke(callbackArg);
            var p = d == null ? new Dictionary<string, object>() : new Dictionary<string, object>(d);
            if (categories != null)
                p.Add("categories", string.Join(",", categories));

            var props = ". {" + string.Join(", ", p.Select(x => x.Key + ": " + x.Value)) + "}";

            SnTrace.Write($"{message}. {props}");
        }


        [Obsolete("Use SnLog.WriteException instead.")]
        public static void WriteException(Exception ex)
        {
            SnLog.WriteException(ex);
        }
        [Obsolete("Use SnLog.WriteException instead.")]
        public static void WriteException(Exception ex, IEnumerable<string> categories)
        {
            SnLog.WriteException(ex, categories: categories);
        }


        [Obsolete("Use the following line: SnLog.WriteAudit(auditEvent, properties);")]
        public static void WriteAudit(AuditEvent auditEvent, IDictionary<string, object> properties)
        {
            SnLog.WriteAudit(auditEvent, properties);
        }
        [Obsolete("Use AuditBlock pattern instead.")]
        public static void WriteUnsuccessfulAudit(AuditEvent auditEvent, string operationMessage, IDictionary<string, object> properties)
        {
            SnLog.WriteAudit(new AuditEvent(auditEvent.AuditCategory, auditEvent.EventId,
                "UNSUCCESSFUL " + auditEvent.AuditCategory, operationMessage), properties);
        }

        /*==========================================================================================*/

        [Obsolete("Do not use anymore.")]
        public static IDictionary<string, object> GetDefaultProperties(object target)
        {
            return Utility.GetDefaultProperties(target);
        }

        [Obsolete("Do not use anymore. Use SenseNet.Configuration.Logging.AuditEnabled instead.")]
        public static bool AuditEnabled => Logging.AuditEnabled;
        #endregion

        private static void WritePrivate(object message, IEnumerable<string> categories = null, int priority = -1, int eventId = 1, TraceEventType severity = TraceEventType.Information, string title = null, IDictionary<string, object> properties = null)
        {
            switch (severity)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    SnLog.WriteError(message, eventId, categories, priority, title, properties);
                    break;
                case TraceEventType.Warning:
                    SnLog.WriteWarning(message, eventId, categories, priority, title, properties);
                    break;
                case TraceEventType.Verbose:
                    // do nothing: verbose log should be written using SnTrace
                    break;
                case TraceEventType.Information:
                    SnLog.WriteInformation(message, eventId, categories, priority, title, properties);
                    break;
                default:
                    SnLog.WriteInformation(severity + ": " + message, eventId, categories, priority, title, properties);
                    break;
            }
        }
    }
}
