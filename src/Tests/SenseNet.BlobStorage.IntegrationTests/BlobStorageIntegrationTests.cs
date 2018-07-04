using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data;

namespace SenseNet.BlobStorage.IntegrationTests
{
    [TestClass]
    public abstract class BlobStorageIntegrationTests
    {
        private static readonly string ConnetionStringBase = @"Data Source=.\SQL2016;Integrated Security=SSPI;Persist Security Info=False";

        private static string GetConnectionString(string databaseName = null)
        {
            return $"Initial Catalog={databaseName};{ConnetionStringBase}";
        }


        protected static void EnsureDatabase(string databaseName)
        {
            var dbid = ExecuteSqlScalar<int?>($"SELECT database_id FROM sys.databases WHERE Name = '{databaseName}'", "master");
            if (dbid == null)
            {
                // CREATE DATABASE
                // load sql file
                var sqlDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    @"..\..\..\..\Storage\Data\SqlClient\Scripts"));
                var sqlPath = Path.Combine(sqlDirectory, "Create_SenseNet_Database_Templated.sql");

throw new NotImplementedException();
            }
        }
        private static void ExecuteSqlCommand(string sql, string databaseName)
        {
            var backup = Configuration.ConnectionStrings.ConnectionString;
            Configuration.ConnectionStrings.ConnectionString = GetConnectionString(databaseName);
            try
            {
                var proc = DataProvider.CreateDataProcedure(sql);
                proc.CommandType = CommandType.Text;
                proc.ExecuteNonQuery();
            }
            finally
            {
                Configuration.ConnectionStrings.ConnectionString = backup;
            }
        }
        private static T ExecuteSqlScalar<T>(string sql, string databaseName)
        {
            var backup = Configuration.ConnectionStrings.ConnectionString;
            Configuration.ConnectionStrings.ConnectionString = GetConnectionString(databaseName);
            try
            {
                var proc = DataProvider.CreateDataProcedure(sql);
                proc.CommandType = CommandType.Text;
                return (T)proc.ExecuteScalar();
            }
            finally
            {
                Configuration.ConnectionStrings.ConnectionString = backup;
            }
        }

        [TestMethod]
        public void Blob_CreateFile()
        {
            Assert.Inconclusive();
        }

    }
}
