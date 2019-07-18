using System;
using System.Data;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;

namespace SenseNet.Packaging.Steps.Internal
{
    public class CheckDatabaseConnection : Step
    {
        public string DataSource { get; set; }
        public string InitialCatalogName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        #region _sqlScript = @"-- create and drop a table for test
        private static readonly string _sqlScript = @"-- create and drop a table for test
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[temptable]') AND type in (N'U'))
BEGIN
	CREATE TABLE [dbo].[temptable]
    (
		[Id] [int] NOT NULL,
		[Name] [varchar](50) NOT NULL
    )
END

insert into [dbo].[temptable] (Id,Name) values (1,'Connection established')
select Name from [dbo].[temptable]

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[temptable]') AND type in (N'U'))
BEGIN
	DROP TABLE [dbo].[temptable]
END
";
        #endregion

        public override void Execute(ExecutionContext context)
        {
            var attempt = 0;
            Exception lastError = null;
            while (++attempt < 132)
            {
                try
                {
                    ExecuteSql(_sqlScript, context);
                    Logger.LogMessage("Connection established.");
                    return;
                }
                catch (Exception e)
                {
                    lastError = e;
                    Logger.LogMessage($"Wait for connection.");
                }
                System.Threading.Thread.Sleep(1000);
            }
            throw new PackagingException("Cannot connect to the database.", lastError);
        }

        private void ExecuteSql(string script, ExecutionContext context)
        {
            //UNDONE:DB: TEST: not tested (packaging)
            var connectionInfo = new ConnectionInfo
            {
                ConnectionName = null,
                DataSource = (string)context.ResolveVariable(DataSource),
                InitialCatalog = InitialCatalog.Initial,
                InitialCatalogName = (string)context.ResolveVariable(InitialCatalogName),
                UserName = (string)context.ResolveVariable(UserName),
                Password = (string)context.ResolveVariable(Password)
            };
            using (var ctx = new MsSqlDataContext_OLD(connectionInfo))
            {
                ctx.ExecuteReaderAsync/*UNDONE*/(script, async (reader, cancel) =>
                {
                    do
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync(cancel))
                            {
                                // empty code block, created only for checking the connection
                            }
                        }
                    } while (reader.NextResult());
                    return Task.FromResult(0);
                }).Wait();
            }
        }
    }
}