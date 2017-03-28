using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.Packaging;

namespace SenseNet.Core.Tests
{
    public class TestDataProcedure : IDataProcedure
    {
        public List<TestDataProcedure> TraceList { get; set; }
        public object ExpectedCommandResult { get; set; }

        public string ExecutorMethod { get; private set; }
        public void Dispose()
        {
            // do nothing
        }

        public CommandType CommandType { get; set; }
        public string CommandText { get; set; }
        public DbParameterCollection Parameters { get; } = new TestDbParameterCollection();
        public void DeriveParameters()
        {
            throw new NotImplementedException();
        }

        public DbDataReader ExecuteReader()
        {
            ExecutorMethod = "ExecuteReader";
            TraceList.Add(this);
            return new TestDataReader();
        }

        public DbDataReader ExecuteReader(CommandBehavior behavior)
        {
            ExecutorMethod = $"ExecuteReader(CommandBehavior.{behavior})";
            TraceList.Add(this);
            return new TestDataReader();
        }

        public object ExecuteScalar()
        {
            ExecutorMethod = "ExecuteScalar";
            TraceList.Add(this);
            return ExpectedCommandResult;
        }

        public int ExecuteNonQuery()
        {
            ExecutorMethod = "ExecuteNonQuery";
            TraceList.Add(this);
            return 0;
        }
    }

    public class TestDbParameterCollection : DbParameterCollection
    {
        private List<DbParameter> _parameters = new List<DbParameter>();

        public override int Add(object value)
        {
            var p = value as DbParameter;
            _parameters.Add(p);
            return _parameters.Count - 1;
        }

        public override bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            throw new NotImplementedException();
        }

        public override int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public override void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public override void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAt(string parameterName)
        {
            throw new NotImplementedException();
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            throw new NotImplementedException();
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            throw new NotImplementedException();
        }

        public override int Count { get { return _parameters.Count; } }
        public override object SyncRoot { get; }

        public override int IndexOf(string parameterName)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        protected override DbParameter GetParameter(int index)
        {
            return _parameters[index];
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(string value)
        {
            throw new NotImplementedException();
        }

        public override void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public override void AddRange(Array values)
        {
            throw new NotImplementedException();
        }
    }

    public class TestDataReader : DbDataReader
    {
        public override string GetName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int FieldCount { get; }

        public override object this[int ordinal]
        {
            get { throw new NotImplementedException(); }
        }

        public override object this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        public override bool HasRows { get; }
        public override bool IsClosed { get; }
        public override int RecordsAffected { get; }

        public override bool NextResult()
        {
            throw new NotImplementedException();
        }

        public override bool Read()
        {
            throw new NotImplementedException();
        }

        public override int Depth { get; }

        public override int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public override bool GetBoolean(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override short GetInt16(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetInt32(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetInt64(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetString(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override decimal GetDecimal(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override double GetDouble(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override Type GetFieldType(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class TestDataProcedureFactory : IDataProcedureFactory
    {
        public List<TestDataProcedure> Procedures { get; }
        public object ExpectedCommandResult { get; set; }

        public TestDataProcedureFactory(List<TestDataProcedure> procedures)
        {
            Procedures = procedures;
        }

        public IDataProcedure CreateProcedure()
        {
            var proc = new TestDataProcedure();
            proc.TraceList = Procedures;
            proc.ExpectedCommandResult = ExpectedCommandResult;
            return proc;
        }
    }

    [TestClass]
    public class PackagingStorageTests
    {
        private static List<TestDataProcedure> Procedures { get; } = new List<TestDataProcedure>();
        private static TestDataProcedureFactory Factory { get; } = new TestDataProcedureFactory(Procedures);
        private static object ExpectedCommandResult {
            get { return Factory.ExpectedCommandResult; }
            set { Factory.ExpectedCommandResult = value; }
        }

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var sqlProvider = new SqlProvider();
            sqlProvider.DataProcedureFactory = Factory;
            var dataProviderAcc = new PrivateType(typeof(DataProvider));
            dataProviderAcc.SetStaticField("_current", sqlProvider);
        }

        [TestMethod]
        public void Packaging_Storage_SavePackage()
        {
            Procedures.Clear();
            ExpectedCommandResult = 142;
            var package = new Package
            {
                Description = "desctription",
                ReleaseDate = DateTime.UtcNow.AddTicks(-555000),
                PackageLevel = PackageLevel.Install,
                AppId = "MyCompany.MyComponent",
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = ExecutionResult.Unfinished,
                ApplicationVersion = new Version(2, 3),
                ExecutionError = null
            };

            // action
            PackageManager.Storage.SavePackage(package);

            // check
            Assert.AreEqual(package.Id, 142);

            var proc = Procedures.FirstOrDefault();
            Assert.IsNotNull(proc);
            Assert.AreEqual(@"INSERT INTO Packages
    (  Name,  Edition,  Description,  AppId,  PackageLevel,  PackageType,  ReleaseDate,  ExecutionDate,  ExecutionResult,  ExecutionError,  AppVersion,  SenseNetVersion) VALUES
    ( @Name, @Edition, @Description, @AppId, @PackageLevel, @PackageType, @ReleaseDate, @ExecutionDate, @ExecutionResult, @ExecutionError, @AppVersion, @SenseNetVersion)
SELECT @@IDENTITY", proc.CommandText);

            var parameters = proc.Parameters;
            Assert.AreEqual(11, parameters.Count);

            CheckParameter(parameters[0], "@Edition", DbType.String, 450, DBNull.Value);
            CheckParameter(parameters[1], "@Description", DbType.String, 1000, package.Description);
            CheckParameter(parameters[2], "@AppId", DbType.AnsiString, 50, package.AppId);
            CheckParameter(parameters[3], "@PackageLevel", DbType.AnsiString, 50, package.PackageLevel.ToString());
            CheckParameter(parameters[4], "@PackageType", DbType.AnsiString, 50, string.Empty);
            CheckParameter(parameters[5], "@ReleaseDate", DbType.DateTime, package.ReleaseDate);
            CheckParameter(parameters[6], "@ExecutionDate", DbType.DateTime, package.ExecutionDate);
            CheckParameter(parameters[7], "@ExecutionResult", DbType.AnsiString, 50, package.ExecutionResult.ToString());
            CheckParameter(parameters[8], "@ExecutionError", DbType.String, 0, DBNull.Value);
            CheckParameter(parameters[9], "@AppVersion", DbType.AnsiString, 50, EncodePackageVersion(package.ApplicationVersion));
            CheckParameter(parameters[10], "@SenseNetVersion", DbType.AnsiString, 50, string.Empty);
        }

        [TestMethod]
        public void Packaging_Storage_UpdatePackage()
        {
            Procedures.Clear();
            var package = new Package
            {
                Description = "desctription",
                ReleaseDate = DateTime.UtcNow.AddTicks(-555000),
                PackageLevel = PackageLevel.Install,
                AppId = "MyCompany.MyComponent",
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = ExecutionResult.Unfinished,
                ApplicationVersion = new Version(2, 3),
                ExecutionError = null
            };

            // action
            PackageManager.Storage.UpdatePackage(package);

            // check
            var proc = Procedures.FirstOrDefault();
            Assert.IsNotNull(proc);
            Assert.AreEqual(@"UPDATE Packages
    SET AppId = @AppId,
        Name = @Name,
        Edition = @Edition,
        Description = @Description,
        PackageLevel = @PackageLevel,
        PackageType = @PackageType,
        ReleaseDate = @ReleaseDate,
        ExecutionDate = @ExecutionDate,
        ExecutionResult = @ExecutionResult,
        ExecutionError = @ExecutionError,
        AppVersion = @AppVersion,
        SenseNetVersion = @SenseNetVersion
WHERE Id = @Id
", proc.CommandText);

            var parameters = proc.Parameters;
            Assert.AreEqual(13, parameters.Count);

            CheckParameter(parameters[0], "@Id", DbType.Int32, package.Id);
            CheckParameter(parameters[1], "@Name", DbType.String, 450, string.Empty);
            CheckParameter(parameters[2], "@Edition", DbType.String, 450, DBNull.Value);
            CheckParameter(parameters[3], "@Description", DbType.String, 1000, package.Description);
            CheckParameter(parameters[4], "@AppId", DbType.AnsiString, 50, package.AppId);
            CheckParameter(parameters[5], "@PackageLevel", DbType.AnsiString, 50, package.PackageLevel.ToString());
            CheckParameter(parameters[6], "@PackageType", DbType.AnsiString, 50, string.Empty);
            CheckParameter(parameters[7], "@ReleaseDate", DbType.DateTime, package.ReleaseDate);
            CheckParameter(parameters[8], "@ExecutionDate", DbType.DateTime, package.ExecutionDate);
            CheckParameter(parameters[9], "@ExecutionResult", DbType.AnsiString, 50, package.ExecutionResult.ToString());
            CheckParameter(parameters[10], "@ExecutionError", DbType.String, 0, DBNull.Value);
            CheckParameter(parameters[11], "@AppVersion", DbType.AnsiString, 50, EncodePackageVersion(package.ApplicationVersion));
            CheckParameter(parameters[12], "@SenseNetVersion", DbType.AnsiString, 50, string.Empty);
        }

        // ================================================================================================== Tools
        private void CheckParameter(DbParameter p, string name, DbType dbType, int size, DBNull value)
        {
            Assert.AreEqual(name, p.ParameterName);
            Assert.AreEqual(dbType, p.DbType);
            Assert.AreEqual(size, p.Size);
            Assert.AreEqual(value, p.Value);
        }
        private void CheckParameter(DbParameter p, string name, DbType dbType, int size, string value)
        {
            Assert.AreEqual(name, p.ParameterName);
            Assert.AreEqual(dbType, p.DbType);
            Assert.AreEqual(size, p.Size);
            Assert.AreEqual(value, p.Value);
        }
        private void CheckParameter(DbParameter p, string name, DbType dbType, DateTime value)
        {
            Assert.AreEqual(name, p.ParameterName);
            Assert.AreEqual(dbType, p.DbType);
            Assert.AreEqual(value, Convert.ToDateTime(p.Value));
        }
        private void CheckParameter(DbParameter p, string name, DbType dbType, int value)
        {
            Assert.AreEqual(name, p.ParameterName);
            Assert.AreEqual(dbType, p.DbType);
            Assert.AreEqual(value, Convert.ToInt32(p.Value));
        }

        private string EncodePackageVersion(Version v)
        {
            if (v.Build < 0)
                return $"{v.Major:0#########}.{v.Minor:0#########}";
            if (v.Revision < 0)
                return $"{v.Major:0#########}.{v.Minor:0#########}.{v.Build:0#########}";
            return $"{v.Major:0#########}.{v.Minor:0#########}.{v.Build:0#########}.{v.Revision:0#########}";
        }

    }
}
