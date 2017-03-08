using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Globalization;

namespace SenseNet.Services.Instrumentation
{
    /// <summary>
    /// The TraceHelper is a static class provides several methods to help you trace your application.
    /// </summary>
    public static class TraceHelper
    {

        public const string DEFAULT_SOURCE_NAME = "SnGlobal";


        #region Trace-related "cache" objects and the corresponding locks ------------------------

        private static Dictionary<MethodBase, string> _traceSourceNames =
            new Dictionary<MethodBase, string>();

        private static Dictionary<MethodBase, string> _methodSignatures =
            new Dictionary<MethodBase, string>();

        private static Dictionary<string, TraceSource> _traceSources =
            new Dictionary<string, TraceSource>();


        private static ReaderWriterLockSlim _traceSourceNamesLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private static ReaderWriterLockSlim _traceSourcesLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private static ReaderWriterLockSlim _methodSignaturesLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        #endregion


        #region Published TraceSource functionality ----------------------------------------------

        /// <summary>
        /// Writes a trace event message to the trace listeners in the Listeners collection using the specified event type and event identifier.
        /// </summary>
        /// <param name="eventType">One of the TraceEventType values that specifies the event type of the trace data.</param>
        /// <param name="id">A numeric identifier for the event.</param>
        public static void TraceEvent(TraceEventType eventType, int id)
        {
            MethodBase caller = GetCallerInfo();
            TraceSource ts = GetTraceSource(caller);
            ts.TraceEvent(eventType, id);
        }


        /// <summary>
        /// Writes a trace event message to the trace listeners in the Listeners collection using the specified event type, event identifier, and message.
        /// </summary>
        /// <param name="eventType">One of the TraceEventType values that specifies the event type of the trace data.</param>
        /// <param name="id">A numeric identifier for the event.</param>
        /// <param name="message">The trace message to write.</param>
        public static void TraceEvent(TraceEventType eventType, int id, string message)
        {
            MethodBase caller = GetCallerInfo();
            TraceSource ts = GetTraceSource(caller);
            string signature = GetCallerSignature(caller);

            ts.TraceEvent(eventType, id, GetFormattedMessage(message, signature));
        }

        /// <summary>
        /// Writes trace data to the trace listeners in the Listeners collection.
        /// </summary>
        /// <param name="eventType">One of the TraceEventType values that specifies the event type of the trace data.</param>
        /// <param name="id">A numeric identifier for the event.</param>
        /// <param name="data">The trace data.</param>
        public static void TraceData(TraceEventType eventType, int id, object data)
        {
            MethodBase caller = GetCallerInfo();
            TraceSource ts = GetTraceSource(caller);
            ts.TraceData(eventType, id, data);
        }

        /// <summary>
        /// Writes trace data to the trace listeners in the Listeners collection.
        /// </summary>
        /// <param name="eventType">One of the TraceEventType values that specifies the event type of the trace data.</param>
        /// <param name="id">A numeric identifier for the event.</param>
        /// <param name="data">An object array containing the trace data.</param>
        public static void TraceData(TraceEventType eventType, int id, params object[] data)
        {
            MethodBase caller = GetCallerInfo();
            TraceSource ts = GetTraceSource(caller);
            ts.TraceData(eventType, id, data);
        }

        /// <summary>
        /// Writes an informational message to the trace listeners in the Listeners collection using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write.</param>
        public static void TraceInformation(string message)
        {
            MethodBase caller = GetCallerInfo();
            TraceInformationPrivate(message, caller);
        }

        /// <summary>
        ///  Writes a trace transfer message to the trace listeners in the Listeners collection using the specified numeric identifier, message, and related activity identifier. 
        /// </summary>
        /// <param name="id">A numeric identifier for the event.</param>
        /// <param name="message">The trace message to write.</param>
        /// <param name="relatedActivity">A Guid structure that identifies the related activity.</param>
        public static void TraceTransfer(int id, string message, Guid relatedActivity)
        {
            MethodBase caller = GetCallerInfo();
            TraceSource ts = GetTraceSource(caller);
            string signature = GetCallerSignature(caller);

            string formattedMessage = GetFormattedMessage(message, signature);

            ts.TraceTransfer(id, formattedMessage, relatedActivity);
        }


        private static void TraceInformationPrivate(string message, MethodBase caller)
        {
            TraceSource ts = GetTraceSource(caller);
            string signature = GetCallerSignature(caller);
            string formattedMessage = GetFormattedMessage(message, signature);
            ts.TraceInformation(formattedMessage);
        }

        
        #endregion




        #region Private helper methods -----------------------------------------------------------


        private static string GetFormattedMessage(string message, string methodSignature)
        {
            string traceFrameString = TraceFrame.GetTraceFrameString();
            string dateTimeString = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            string managedThreadIdString = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
            string formattedMessage = string.Format(CultureInfo.InvariantCulture, "[{0}] '{1}' [{2}] [{3}] [{4}]", traceFrameString, message, methodSignature, dateTimeString, managedThreadIdString);
            return formattedMessage;
        }

        internal static MethodBase GetCallerInfo()
        {
            StackTrace st = new StackTrace(false);
            MethodBase callerMethod = st.GetFrame(2).GetMethod(); // The caller of the caller
            return callerMethod;
        }

        private static TraceSource GetTraceSource(MethodBase caller)
        {

            string traceSourceName = null;

            _traceSourceNamesLock.EnterReadLock();
            bool traceSourceNameFound = _traceSourceNames.TryGetValue(caller, out traceSourceName);
            _traceSourceNamesLock.ExitReadLock();

            if (!traceSourceNameFound)
            {
                // if the trace source name cannot be found in the "method - trace source name" cache,
                // try to find the source name on the calling method

                object[] customAttributes;

                customAttributes = caller.GetCustomAttributes(typeof(TraceSourceNameAttribute), false);
                if (customAttributes.Length == 1)
                    traceSourceName = ((TraceSourceNameAttribute)customAttributes[0]).TraceSourceName;


                // if not found on the method name, and the method is a property getter or setter try the prop.
                if (string.IsNullOrEmpty(traceSourceName) && (caller.Name.StartsWith("get_", StringComparison.Ordinal) || caller.Name.StartsWith("set_", StringComparison.Ordinal)))
                {
                    string propertyName = caller.Name.Substring(4);
                    customAttributes = caller.DeclaringType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetCustomAttributes(typeof(TraceSourceNameAttribute), false);

                    if (customAttributes.Length == 1)
                        traceSourceName = ((TraceSourceNameAttribute)customAttributes[0]).TraceSourceName;

                }

                // if not found on the method name, try to find on the class
                if (string.IsNullOrEmpty(traceSourceName))
                {
                    customAttributes = caller.DeclaringType.GetCustomAttributes(typeof(TraceSourceNameAttribute), false);
                    if (customAttributes.Length == 1)
                        traceSourceName = ((TraceSourceNameAttribute)customAttributes[0]).TraceSourceName;
                }

                // if not found on the class name, try to find on the assembly
                if (string.IsNullOrEmpty(traceSourceName))
                {
                    customAttributes = caller.DeclaringType.Assembly.GetCustomAttributes(typeof(TraceSourceNameAttribute), false);
                    if (customAttributes.Length == 1)
                        traceSourceName = ((TraceSourceNameAttribute)customAttributes[0]).TraceSourceName;
                }

                if (string.IsNullOrEmpty(traceSourceName))
                    traceSourceName = DEFAULT_SOURCE_NAME;

                // add to cache
                _traceSourceNamesLock.EnterWriteLock();
                _traceSourceNames.Add(caller, traceSourceName);
                _traceSourceNamesLock.ExitWriteLock();
            }

            TraceSource traceSource = null;

            _traceSourcesLock.EnterReadLock();
            bool traceSourceFound = _traceSources.TryGetValue(traceSourceName, out traceSource);
            _traceSourcesLock.ExitReadLock();

            if (!traceSourceFound)
            {
                traceSource = new TraceSource(traceSourceName, SourceLevels.All);

                _traceSourcesLock.EnterWriteLock();
                _traceSources.Add(traceSourceName, traceSource);
                _traceSourcesLock.ExitWriteLock();
            }

            return traceSource;

        }

        private static string GetCallerSignature(MethodBase caller)
        {
            string signature;

            _methodSignaturesLock.EnterReadLock();
            bool signatureFound = _methodSignatures.TryGetValue(caller, out signature);
            _methodSignaturesLock.ExitReadLock();

            if (!signatureFound)
            {
                System.Text.StringBuilder signatureBuilder = new StringBuilder();

                if (caller.IsPrivate) signatureBuilder.Append("private ");
                if (caller.IsPublic) signatureBuilder.Append("public ");
                if (caller.IsStatic) signatureBuilder.Append("static ");
                if (caller.IsAbstract) signatureBuilder.Append("abstract ");
                if (caller.IsVirtual) signatureBuilder.Append("virtual ");

                MethodInfo mi = caller as MethodInfo;
                ConstructorInfo ci = caller as ConstructorInfo;

                if (mi != null)
                    signatureBuilder.Append(mi.ReturnType.Name);
                if (ci != null)
                    signatureBuilder.Append(ci.DeclaringType.Name);

                signatureBuilder.Append(" ");
                signatureBuilder.Append(caller.Name);

                if (mi != null)
                {
                    Type[] genericArgumentTypes = mi.GetGenericArguments();

                    if (genericArgumentTypes != null && genericArgumentTypes.Length > 0)
                    {
                        signatureBuilder.Append("<");
                        bool firstGenericArgument = true;
                        foreach (Type genericArgumentType in genericArgumentTypes)
                        {
                            if (!firstGenericArgument)
                                signatureBuilder.Append(", ");
                            else
                                firstGenericArgument = false;

                            signatureBuilder.Append(genericArgumentType.Name);

                        }
                        signatureBuilder.Append(">");
                    }
                }

                signatureBuilder.Append("(");

                bool firstParameter = true;
                foreach (ParameterInfo paremeter in caller.GetParameters())
                {
                    if (!firstParameter)
                        signatureBuilder.Append(", ");
                    else
                        firstParameter = false;

                    signatureBuilder.Append(paremeter.ParameterType.Name);
                    signatureBuilder.Append(" ");
                    signatureBuilder.Append(paremeter.Name);
                }

                signatureBuilder.Append(")");

                signature = signatureBuilder.ToString();

                _methodSignaturesLock.EnterWriteLock();
                _methodSignatures.Add(caller, signature);
                _methodSignaturesLock.ExitWriteLock();
            }

            return signature;
        }

        
        #endregion


        #region TraceFrame functions -------------------------------------------------------------



        internal static void TraceFrameEvent(TraceFrameEventType eventType, TraceFrame traceFrame)
        {
            string traceFrameString = TraceFrame.GetTraceFrameString();
            string fullMessage = string.Concat(traceFrameString, " ", traceFrame.Message, " ", eventType.ToString());

            MethodBase caller = traceFrame.Caller;

            TraceHelper.TraceInformationPrivate(fullMessage, caller);
        }

        #endregion

    }
}
