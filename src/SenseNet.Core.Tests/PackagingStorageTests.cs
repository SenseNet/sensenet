﻿using System;
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
            set { Factory.ExpectedCommandResult = value; }
        }

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            PackageManager.StorageFactory = new BuiltinPackageStorageProviderFactory();

            var sqlProvider = new SqlProvider();
            sqlProvider.DataProcedureFactory = Factory;
            var dataProviderAcc = new PrivateType(typeof(DataProvider));
            dataProviderAcc.SetStaticField("_current", sqlProvider);
        }

        /* ================================================================================================== Tests */

        [TestMethod]
        public void Packaging_Storage_LoadInstalledApplications()
        {
            Procedures.Clear();
            ExpectedCommandResult = new TestDataReader(new[] { "ComponentId", "AppVersion", "AcceptableVersion", "Description" },
                new[]
                {
                    new object[] { "Component1", "0000000002.0000000000", "0000000001.0000000002"           , "description1" },
                    new object[] { "Component2", "0000000003.0000000008", "0000000003.0000000007.0000000042", "description2" },
                    new object[] { "Component3", "0000000006.0000000007", "0000000006.0000000005",            "description3" },
                });

            // action
            var result = PackageManager.Storage.LoadInstalledApplications();

            // check
            Assert.AreEqual(1, Procedures.Count);
            var proc = Procedures[0];
            Assert.AreEqual(@"SELECT P2.Description, P1.ComponentId, P1.AppVersion, P1a.AppVersion AcceptableVersion
FROM (SELECT ComponentId, MAX(AppVersion) AppVersion FROM Packages WHERE ComponentId IS NOT NULL GROUP BY ComponentId) P1
JOIN (SELECT ComponentId, MAX(AppVersion) AppVersion FROM Packages WHERE ComponentId IS NOT NULL 
    AND ExecutionResult != '" + ExecutionResult.Faulty.ToString() + @"'
    AND ExecutionResult != '" + ExecutionResult.Unfinished.ToString() + @"' GROUP BY ComponentId, ExecutionResult) P1a
ON P1.ComponentId = P1a.ComponentId
JOIN (SELECT Description, ComponentId FROM Packages WHERE PackageLevel = '" + PackageType.Install.ToString() + @"'
    AND ExecutionResult != '" + ExecutionResult.Faulty.ToString() + @"'
    AND ExecutionResult != '" + ExecutionResult.Unfinished.ToString() + @"') P2
ON P1.ComponentId = P2.ComponentId", proc.CommandText);

            var parameters = proc.Parameters;
            Assert.AreEqual(0, parameters.Count);

            Assert.AreEqual("ExecuteReader", proc.ExecutorMethod);

            // check applications
            var applications = result.ToArray();
            Assert.AreEqual(3, applications.Length);

            Assert.AreEqual("Component1", applications[0].ComponentId);
            Assert.AreEqual("2.0", applications[0].Version.ToString());
            Assert.AreEqual("1.2", applications[0].AcceptableVersion.ToString());
            Assert.AreEqual("description1", applications[0].Description);

            Assert.AreEqual("Component2", applications[1].ComponentId);
            Assert.AreEqual("3.8", applications[1].Version.ToString());
            Assert.AreEqual("3.7.42", applications[1].AcceptableVersion.ToString());
            Assert.AreEqual("description2", applications[1].Description);

            Assert.AreEqual("Component3", applications[2].ComponentId);
            Assert.AreEqual("6.7", applications[2].Version.ToString());
            Assert.AreEqual("6.5", applications[2].AcceptableVersion.ToString());
            Assert.AreEqual("description3", applications[2].Description);

        }

        [TestMethod]
        public void Packaging_Storage_LoadInstalledPackages_Empty()
        {
            Procedures.Clear();
            ExpectedCommandResult = new TestDataReader();

            // action
            var result = PackageManager.Storage.LoadInstalledPackages();

            // check
            Assert.IsFalse(result.Any());

            Assert.AreEqual(1, Procedures.Count);
            var proc = Procedures[0];
            Assert.AreEqual(@"SELECT * FROM Packages", proc.CommandText);

            var parameters = proc.Parameters;
            Assert.AreEqual(0, parameters.Count);

            Assert.AreEqual("ExecuteReader", proc.ExecutorMethod);
        }

        [TestMethod]
        public void Packaging_Storage_LoadInstalledPackages()
        {
            Procedures.Clear();

            var executionDate3 = DateTime.UtcNow;
            var executionDate2 = executionDate3.AddDays(-1);
            var executionDate1 = executionDate2.AddDays(-1);
            var releaseDate3 = executionDate1.AddDays(-1);
            var releaseDate2 = releaseDate3.AddDays(-1);
            var releaseDate1 = releaseDate2.AddDays(-1);

            ExpectedCommandResult = new TestDataReader(new [] { "Id","Name","PackageType","PackageLevel","SenseNetVersion",
                "ComponentId","AppVersion","ReleaseDate","ExecutionDate","ExecutionResult","ExecutionError","Description" },
                new []
                {
                    new object[] {1, null, "", "Install", "", "Component1", "0000000001.0000000002"           , releaseDate1, executionDate1, "Successful", null, "description1" },
                    new object[] {2, null, "", "Install", "", "Component2", "0000000003.0000000007.0000000042", releaseDate2, executionDate2, "Successful", null, "description2" },
                    new object[] {3, null, "", "Patch",   "", "Component2", "0000000006.0000000005",            releaseDate3, executionDate3, "Successful", null, "description3" },
                });

            // action
            var result = PackageManager.Storage.LoadInstalledPackages();

            // check
            Assert.AreEqual(1, Procedures.Count);
            var proc = Procedures[0];
            Assert.AreEqual(@"SELECT * FROM Packages", proc.CommandText);

            var parameters = proc.Parameters;
            Assert.AreEqual(0, parameters.Count);

            Assert.AreEqual("ExecuteReader", proc.ExecutorMethod);

            // check packages
            var packages = result.ToArray();
            Assert.AreEqual(3, packages.Length);

            Assert.AreEqual(1, packages[0].Id);
            Assert.AreEqual(PackageType.Install, packages[0].PackageType);
            Assert.AreEqual("Component1", packages[0].ComponentId);
            Assert.AreEqual("1.2", packages[0].ApplicationVersion.ToString());
            Assert.AreEqual(releaseDate1, packages[0].ReleaseDate);
            Assert.AreEqual(executionDate1, packages[0].ExecutionDate);
            Assert.AreEqual(ExecutionResult.Successful, packages[0].ExecutionResult);
            Assert.AreEqual(null, packages[0].ExecutionError);
            Assert.AreEqual("description1", packages[0].Description);

            Assert.AreEqual(2, packages[1].Id);
            Assert.AreEqual(PackageType.Install, packages[1].PackageType);
            Assert.AreEqual("Component2", packages[1].ComponentId);
            Assert.AreEqual("3.7.42", packages[1].ApplicationVersion.ToString());
            Assert.AreEqual(releaseDate2, packages[1].ReleaseDate);
            Assert.AreEqual(executionDate2, packages[1].ExecutionDate);
            Assert.AreEqual(ExecutionResult.Successful, packages[1].ExecutionResult);
            Assert.AreEqual(null, packages[1].ExecutionError);
            Assert.AreEqual("description2", packages[1].Description);

            Assert.AreEqual(3, packages[2].Id);
            Assert.AreEqual(PackageType.Patch, packages[2].PackageType);
            Assert.AreEqual("Component2", packages[2].ComponentId);
            Assert.AreEqual("6.5", packages[2].ApplicationVersion.ToString());
            Assert.AreEqual(releaseDate3, packages[2].ReleaseDate);
            Assert.AreEqual(executionDate3, packages[2].ExecutionDate);
            Assert.AreEqual(ExecutionResult.Successful, packages[2].ExecutionResult);
            Assert.AreEqual(null, packages[2].ExecutionError);
            Assert.AreEqual("description3", packages[2].Description);
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
                PackageType = PackageType.Install,
                ComponentId = "MyCompany.MyComponent",
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = ExecutionResult.Unfinished,
                ApplicationVersion = new Version(2, 3),
                ExecutionError = null
            };

            // action
            PackageManager.Storage.SavePackage(package);

            // check
            Assert.AreEqual(package.Id, 142);

            Assert.AreEqual(1, Procedures.Count);
            var proc = Procedures[0];

            Assert.AreEqual(@"INSERT INTO Packages
    (  Description,  ComponentId,  PackageLevel,  PackageType,  ReleaseDate,  ExecutionDate,  ExecutionResult,  ExecutionError,  AppVersion,  SenseNetVersion) VALUES
    ( @Description, @ComponentId, @PackageLevel, @PackageType, @ReleaseDate, @ExecutionDate, @ExecutionResult, @ExecutionError, @AppVersion, @SenseNetVersion)
SELECT @@IDENTITY", proc.CommandText);

            var parameters = proc.Parameters;
            Assert.AreEqual(10, parameters.Count);

            CheckParameter(parameters[0], "@Description", DbType.String, 1000, package.Description);
            CheckParameter(parameters[1], "@ComponentId", DbType.AnsiString, 50, package.ComponentId);
            CheckParameter(parameters[2], "@PackageLevel", DbType.AnsiString, 50, package.PackageType.ToString());
            CheckParameter(parameters[3], "@PackageType", DbType.AnsiString, 50, string.Empty);
            CheckParameter(parameters[4], "@ReleaseDate", DbType.DateTime, package.ReleaseDate);
            CheckParameter(parameters[5], "@ExecutionDate", DbType.DateTime, package.ExecutionDate);
            CheckParameter(parameters[6], "@ExecutionResult", DbType.AnsiString, 50, package.ExecutionResult.ToString());
            CheckParameter(parameters[7], "@ExecutionError", DbType.String, 0, DBNull.Value);
            CheckParameter(parameters[8], "@AppVersion", DbType.AnsiString, 50, EncodePackageVersion(package.ApplicationVersion));
            CheckParameter(parameters[9], "@SenseNetVersion", DbType.AnsiString, 50, string.Empty);

            Assert.AreEqual("ExecuteScalar", proc.ExecutorMethod);
        }

        [TestMethod]
        public void Packaging_Storage_UpdatePackage()
        {
            Procedures.Clear();
            var package = new Package
            {
                Description = "desctription",
                ReleaseDate = DateTime.UtcNow.AddTicks(-555000),
                PackageType = PackageType.Install,
                ComponentId = "MyCompany.MyComponent",
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = ExecutionResult.Unfinished,
                ApplicationVersion = new Version(2, 3),
                ExecutionError = null
            };

            // action
            PackageManager.Storage.UpdatePackage(package);

            // check
            Assert.AreEqual(1, Procedures.Count);
            var proc = Procedures[0];

            Assert.AreEqual(@"UPDATE Packages
    SET ComponentId = @ComponentId,
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
            Assert.AreEqual(11, parameters.Count);

            CheckParameter(parameters[0], "@Id", DbType.Int32, package.Id);
            CheckParameter(parameters[1], "@Description", DbType.String, 1000, package.Description);
            CheckParameter(parameters[2], "@ComponentId", DbType.AnsiString, 50, package.ComponentId);
            CheckParameter(parameters[3], "@PackageLevel", DbType.AnsiString, 50, package.PackageType.ToString());
            CheckParameter(parameters[4], "@PackageType", DbType.AnsiString, 50, string.Empty);
            CheckParameter(parameters[5], "@ReleaseDate", DbType.DateTime, package.ReleaseDate);
            CheckParameter(parameters[6], "@ExecutionDate", DbType.DateTime, package.ExecutionDate);
            CheckParameter(parameters[7], "@ExecutionResult", DbType.AnsiString, 50, package.ExecutionResult.ToString());
            CheckParameter(parameters[8], "@ExecutionError", DbType.String, 0, DBNull.Value);
            CheckParameter(parameters[9], "@AppVersion", DbType.AnsiString, 50, EncodePackageVersion(package.ApplicationVersion));
            CheckParameter(parameters[10], "@SenseNetVersion", DbType.AnsiString, 50, string.Empty);

            Assert.AreEqual("ExecuteNonQuery", proc.ExecutorMethod);
        }

        [TestMethod]
        public void Packaging_Storage_IsPackageExist_False()
        {
            Procedures.Clear();
            ExpectedCommandResult = 0;
            var version = new Version(3, 7, 42);

            // action
            var result = PackageManager.Storage.IsPackageExist("Component1", PackageType.Install, version);

            // check
            Assert.IsFalse(result);

            Assert.AreEqual(1, Procedures.Count);
            var proc = Procedures[0];

            Assert.AreEqual(@"SELECT COUNT(0) FROM Packages
WHERE ComponentId = @ComponentId AND PackageLevel = @PackageLevel AND SenseNetVersion = @Version
", proc.CommandText);

            var parameters = proc.Parameters;
            Assert.AreEqual(3, parameters.Count);

            CheckParameter(parameters[0], "@ComponentId", DbType.AnsiString, 50, "Component1");
            CheckParameter(parameters[1], "@PackageLevel", DbType.AnsiString, 50, "Install");
            CheckParameter(parameters[2], "@Version", DbType.AnsiString, 50, EncodePackageVersion(version));

            Assert.AreEqual("ExecuteScalar", proc.ExecutorMethod);
        }

        [TestMethod]
        public void Packaging_Storage_IsPackageExist_True()
        {
            Procedures.Clear();
            ExpectedCommandResult = 7;
            var version = new Version(3, 7, 42);

            // action
            var result = PackageManager.Storage.IsPackageExist("Component1", PackageType.Install, version);

            // check
            Assert.IsTrue(result);

            Assert.AreEqual(1, Procedures.Count);
            var proc = Procedures[0];

            Assert.AreEqual(@"SELECT COUNT(0) FROM Packages
WHERE ComponentId = @ComponentId AND PackageLevel = @PackageLevel AND SenseNetVersion = @Version
", proc.CommandText);

            var parameters = proc.Parameters;
            Assert.AreEqual(3, parameters.Count);

            CheckParameter(parameters[0], "@ComponentId", DbType.AnsiString, 50, "Component1");
            CheckParameter(parameters[1], "@PackageLevel", DbType.AnsiString, 50, "Install");
            CheckParameter(parameters[2], "@Version", DbType.AnsiString, 50, EncodePackageVersion(version));

            Assert.AreEqual("ExecuteScalar", proc.ExecutorMethod);
        }

        [TestMethod]
        public void Packaging_Storage_DeletePackage()
        {
            var packageId = 142;

            Procedures.Clear();
            var package = new Package
            {
                Id = packageId,
                Description = "desctription",
                ReleaseDate = DateTime.UtcNow.AddTicks(-555000),
                PackageType = PackageType.Install,
                ComponentId = "MyCompany.MyComponent",
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = ExecutionResult.Unfinished,
                ApplicationVersion = new Version(2, 3),
                ExecutionError = null
            };

            // action
            PackageManager.Storage.DeletePackage(package);

            // check
            Assert.AreEqual(1, Procedures.Count);
            var proc = Procedures[0];

            Assert.AreEqual(@"DELETE FROM Packages WHERE Id = @Id", proc.CommandText);

            var parameters = proc.Parameters;
            Assert.AreEqual(1, parameters.Count);

            CheckParameter(parameters[0], "@Id", DbType.Int32, packageId);

            Assert.AreEqual("ExecuteNonQuery", proc.ExecutorMethod);
        }
        [TestMethod]
        public void Packaging_Storage_DeleteUnsavedPackage_Throws()
        {
            Procedures.Clear();
            var package = new Package
            {
                Description = "desctription",
                ReleaseDate = DateTime.UtcNow.AddTicks(-555000),
                PackageType = PackageType.Install,
                ComponentId = "MyCompany.MyComponent",
                ExecutionDate = DateTime.UtcNow,
                ExecutionResult = ExecutionResult.Unfinished,
                ApplicationVersion = new Version(2, 3),
                ExecutionError = null
            };

            // action
            var errorMessage = (string)null;
            try
            {
                PackageManager.Storage.DeletePackage(package);
                Assert.Fail("Exception was not thrown.");
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
            }

            // check
            Assert.AreEqual("Cannot delete unsaved package", errorMessage);
        }

        [TestMethod]
        public void Packaging_Storage_DeletePackagesExceptFirst()
        {
            Procedures.Clear();

            // action
            PackageManager.Storage.DeletePackagesExceptFirst();

            // check
            Assert.AreEqual(1, Procedures.Count);
            var proc = Procedures[0];
            Assert.AreEqual(@"DELETE FROM Packages WHERE Id > 1", proc.CommandText);

            var parameters = proc.Parameters;
            Assert.AreEqual(0, parameters.Count);

            Assert.AreEqual("ExecuteNonQuery", proc.ExecutorMethod);
        }

        /* ================================================================================================== Tools */

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
