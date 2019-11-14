using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Packaging.Steps;
using SenseNet.Tools;

namespace SenseNet.Packaging
{
    public enum LogLevel { Default, File, Console, Silent }

    public interface IPackagingLogger
    {
        LogLevel AcceptedLevel { get; }
        string LogFilePath { get; }
        void Initialize(LogLevel level, string logFilePath);
        void WriteTitle(string title);
        void WriteMessage(string message);
    }

    public static class Logger
    {
        public static LogLevel Level { get; private set; }
        private static IPackagingLogger[] _loggers = new IPackagingLogger[0];
        public static int Errors { get; set; }
        public static string PackageName { get; set; }

        public static void Create(LogLevel level, string logfilePath = null)
        {
            Level = level;
            _loggers = (from t in TypeResolver.GetTypesByInterface(typeof(IPackagingLogger))
                        select (IPackagingLogger)Activator.CreateInstance(t)).ToArray<IPackagingLogger>();
            foreach (var logger in _loggers)
                logger.Initialize(level, logfilePath);
        }

        public static string GetLogFileName()
        {
            return _loggers.Where(l => l.LogFilePath != null).Select(l => l.LogFilePath).FirstOrDefault();
        }

        public static void LogTitle(string title)
        {
            foreach (var logger in _loggers)
                if (logger.AcceptedLevel >= Level)
                    logger.WriteTitle(title);
        }
        public static void LogMessage(string message)
        {
            foreach (var logger in _loggers)
                if (logger.AcceptedLevel >= Level)
                    logger.WriteMessage(message);
        }
        public static void LogMessage(string format, params object[] parameters)
        {
            var msg = String.Format(format, parameters);
            foreach (var logger in _loggers)
                if (logger.AcceptedLevel >= Level)
                    logger.WriteMessage(msg);
        }
        public static void LogWarningMessage(string message)
        {
            var msg = String.Concat("WARNING: ", message);
            foreach (var logger in _loggers)
                if (logger.AcceptedLevel >= Level)
                    logger.WriteMessage(msg);
        }
        public static void LogException(Exception e)
        {
            LogMessage(PrintException(e, null));
        }
        public static void LogException(Exception e, string prefix)
        {
            LogMessage(PrintException(e, prefix));
        }
        internal static void LogStep(Step step, int maxStepId)
        {
            LogMessage("================================================== #{0}/{1} {2}", step.StepId + 1, maxStepId, step.ElementName);
        }

        private static string PrintException(Exception e, string prefix)
        {
            Errors++;

            StringBuilder sb = new StringBuilder();
            if (prefix != null)
                sb.Append(prefix).Append(": ");

            sb.Append(e.GetType().Name);
            sb.Append(": ");
            sb.AppendLine(e.Message);
            PrintTypeLoadError(e as System.Reflection.ReflectionTypeLoadException, sb);
            sb.AppendLine(e.StackTrace);
            while ((e = e.InnerException) != null)
            {
                sb.AppendLine("---- Inner Exception:");
                sb.Append(e.GetType().Name);
                sb.Append(": ");
                sb.AppendLine(e.Message);
                PrintTypeLoadError(e as System.Reflection.ReflectionTypeLoadException, sb);
                sb.AppendLine(e.StackTrace);
            }
            return sb.ToString();
        }
        private static void PrintTypeLoadError(System.Reflection.ReflectionTypeLoadException exc, StringBuilder sb)
        {
            if (exc == null)
                return;
            sb.AppendLine("LoaderExceptions:");
            foreach (var e in exc.LoaderExceptions)
            {
                sb.Append("-- ");
                sb.Append(e.GetType().FullName);
                sb.Append(": ");
                sb.AppendLine(e.Message);

                var fileNotFoundException = e as System.IO.FileNotFoundException;
                if (fileNotFoundException != null)
                {
                    sb.AppendLine("FUSION LOG:");
                    sb.AppendLine(fileNotFoundException.FusionLog);
                }
            }
        }
    }
}
