using System;
using System.Text;
using System.Data;
using SenseNet.ContentRepository.Storage.Data;
using System.IO;

namespace SenseNet.Packaging.Steps
{
    public class ExecuteDatabaseScript : Step
    {
        private class SqlScriptReader : IDisposable
        {
            private readonly TextReader _reader;
            public string Script { get; private set; }
            public SqlScriptReader(TextReader reader)
            {
                _reader = reader;
            }

            public void Dispose()
            {
                this.Close();
            }
            public virtual void Close()
            {
                _reader.Close();
                GC.SuppressFinalize(this);
            }

            public bool ReadScript()
            {
                var sb = new StringBuilder();

                string line;
                while (true)
                {
                    line = _reader.ReadLine();

                    if (line == null)
                        break;

                    if (String.Equals(line, "GO", StringComparison.OrdinalIgnoreCase))
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

        [DefaultProperty]
        public string Query { get; set; }
        public string ConnectionName { get; set; }
        public string DataSource { get; set; }
        public string InitialCatalogName { get; set; }
        public InitialCatalog InitialCatalog { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public override void Execute(ExecutionContext context)
        {
            string queryPath = null;
            try
            {
                queryPath = ResolvePackagePath(Query, context);
                if(!File.Exists(queryPath))
                    queryPath = null;
            }
            catch
            {
                queryPath = null;
            }

            if (queryPath != null)
                ExecuteFromFile(queryPath, context);
            else
                ExecuteFromText(Query, context);
        }
        private void ExecuteFromFile(string path, ExecutionContext context)
        {
            Logger.LogMessage(path);

            using (var reader = new StreamReader(path))
            using (var sqlReader = new SqlScriptReader(reader))
                ExecuteSql(sqlReader, context);
        }
        private void ExecuteFromText(string text, ExecutionContext context)
        {
            text = text.Trim();
            if (text.StartsWith("<![CDATA[") && text.EndsWith("]]>"))
                text = text.Substring(9, text.Length - 12).Trim();

            using (var reader = new StringReader(text))
            using (var sqlReader = new SqlScriptReader(reader))
                ExecuteSql(sqlReader, context);
        }
        private void ExecuteSql(SqlScriptReader sqlReader, ExecutionContext context)
        {
            while (sqlReader.ReadScript())
            {
                var script = sqlReader.Script;

                var sb = new StringBuilder();
                using (var proc = CreateDataProcedure(script, context))
                {
                    proc.CommandType = CommandType.Text;

                    using (var reader = proc.ExecuteReader())
                    {
                        do
                        {
                            if (reader.HasRows)
                            {
                                var first = true;
                                while (reader.Read())
                                {
                                    if (first)
                                    {
                                        for (int i = 0; i < reader.FieldCount; i++)
                                            sb.Append(reader.GetName(i)).Append("\t");
                                        Logger.LogMessage(sb.ToString());
                                        sb.Clear();
                                        first = false;
                                    }
                                    for (int i = 0; i < reader.FieldCount; i++)
                                        sb.Append(reader[i]).Append("\t");
                                    Logger.LogMessage(sb.ToString());
                                    sb.Clear();
                                }
                            }
                        } while (reader.NextResult());
                    }
                }
            }
            Logger.LogMessage("Script is successfully executed.");
        }

        private IDataProcedure CreateDataProcedure(string script, ExecutionContext context)
        {
            return DataProvider.CreateDataProcedure(script, new ConnectionInfo
            {
                ConnectionName = (string)context.ResolveVariable(ConnectionName),
                DataSource = (string)context.ResolveVariable(DataSource),
                InitialCatalog = InitialCatalog,
                InitialCatalogName = (string)context.ResolveVariable(InitialCatalogName),
                UserName  = (string)context.ResolveVariable(UserName),
                Password = (string)context.ResolveVariable(Password)
            });
        }
    }
}
