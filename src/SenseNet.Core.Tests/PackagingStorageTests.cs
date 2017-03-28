using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.SqlClient;
using SenseNet.Core.Tests.Implementations;
using SenseNet.Packaging;

namespace SenseNet.Core.Tests
{
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

        // ================================================================================================== Tests

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
