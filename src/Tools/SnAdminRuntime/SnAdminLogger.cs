using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Packaging;
using System.IO;
using IO = System.IO;

namespace SenseNet.Tools.SnAdmin
{
    internal class SnAdminLogger : IPackagingLogger
    {
        private const int LINELENGTH = 80;

        protected Dictionary<char, string> _lines;

        public virtual LogLevel AcceptedLevel { get { return LogLevel.File; } }

        public SnAdminLogger()
        {
            _lines = new Dictionary<char, string>();
            _lines['='] = new StringBuilder().Append('=', LINELENGTH - 1).ToString();
            _lines['-'] = new StringBuilder().Append('-', LINELENGTH - 1).ToString();
        }

        public virtual void Initialize(LogLevel level, string logFilePath)
        {
            if (level <= LogLevel.File)
                CreateLog(logFilePath);
        }

        public void WriteTitle(string title)
        {
            LogWriteLine(_lines['=']);
            LogWriteLine(Center(title));
            LogWriteLine(_lines['=']);
        }
        public void WriteMessage(string message)
        {
            LogWriteLine(message);
        }

        private string Center(string text)
        {
            if (text.Length >= LINELENGTH - 1)
                return text;
            var sb = new StringBuilder();
            sb.Append(' ', (LINELENGTH - text.Length) / 2).Append(text);
            return sb.ToString();
        }

        // ================================================================================================================= Logger

        private static readonly string CR = Environment.NewLine;
        public string LogFilePath { get; private set; }

        private string _logFolder = null;
        public string LogFolder
        {
            get
            {
                if (_logFolder == null)
                {
                    _logFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\log\"));
                    if (!Directory.Exists(_logFolder))
                        Directory.CreateDirectory(_logFolder);
                }
                return _logFolder;
            }
            set
            {
                if (!Directory.Exists(value))
                    Directory.CreateDirectory(value);
                _logFolder = value;
            }
        }

        protected bool _lineStart;

        public virtual void LogWrite(params object[] values)
        {
            using (StreamWriter writer = OpenLog())
            {
                WriteToLog(writer, values, false);
            }
            _lineStart = false;
        }
        public virtual void LogWriteLine(params object[] values)
        {
            using (StreamWriter writer = OpenLog())
            {
                WriteToLog(writer, values, true);
            }
            _lineStart = true;
        }
        private void CreateLog(string logfilePath)
        {
            _lineStart = true;
            if (logfilePath != null)
                LogFilePath = logfilePath;
            else
                LogFilePath = Path.Combine(LogFolder, Logger.PackageName + DateTime.UtcNow.ToString("_yyyyMMdd-HHmmss") + ".log");

            if (!IO.File.Exists(LogFilePath))
            {
                using (FileStream fs = new FileStream(LogFilePath, FileMode.Create))
                {
                    using (StreamWriter wr = new StreamWriter(fs))
                    {
                        wr.WriteLine("");
                    }
                }
            }
        }
        private StreamWriter OpenLog()
        {
            return new StreamWriter(LogFilePath, true);
        }
        private void WriteToLog(StreamWriter writer, object[] values, bool newLine)
        {
            if (_lineStart)
            {
                writer.Write(DateTime.UtcNow.ToString("HH:mm:ss.ffff"));
                writer.Write("\t");
            }
            foreach (object value in values)
            {
                writer.Write(value);
            }
            if (newLine)
            {
                writer.WriteLine();
            }
        }
    }
    internal class SnAdminConsoleLogger : SnAdminLogger
    {
        public override LogLevel AcceptedLevel { get { return LogLevel.Console; } }

        public override void Initialize(LogLevel level, string logFilePath) { }

        public override void LogWrite(params object[] values)
        {
            WriteToLog(values, false);
        }
        public override void LogWriteLine(params object[] values)
        {
            WriteToLog(values, true);
        }
        private void WriteToLog(object[] values, bool newLine)
        {
            foreach (object value in values)
                Console.Write(value);
            if (newLine)
                Console.WriteLine();
        }
    }
}
