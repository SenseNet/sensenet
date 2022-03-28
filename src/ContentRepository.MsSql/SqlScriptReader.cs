using System;
using System.IO;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    internal class SqlScriptReader : IDisposable
    {
        private readonly TextReader _reader;
        public string Script { get; private set; }
        public SqlScriptReader(TextReader reader)
        {
            _reader = reader;
        }

        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            _reader.Close();
            GC.SuppressFinalize(this);
        }

        public bool ReadScript()
        {
            var sb = new StringBuilder();

            while (true)
            {
                var line = _reader.ReadLine();

                if (line == null)
                    break;

                if (string.Equals(line, "GO", StringComparison.OrdinalIgnoreCase))
                {
                    Script = sb.ToString();
                    return true;
                }
                sb.AppendLine(line);
            }

            if (sb.Length <= 0)
                return false;

            Script = sb.ToString();
            return true;
        }
    }
}
