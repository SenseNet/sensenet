using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Storage.Data.MsSqlClient;

namespace SenseNet.IntegrationTests.CustomTests
{
    [TestClass]
    public class MsSqlDatabaseInstallerTest
    {
        private static readonly string LocalServer = "(local)\\SQL2016";

        private static MsSqlDatabaseInstaller CreateInstaller(MsSqlDatabaseInstallationOptions options = null)
        {
            return new MsSqlDatabaseInstaller(Options.Create(options ?? new MsSqlDatabaseInstallationOptions()),
                NullLoggerFactory.Instance.CreateLogger<MsSqlDatabaseInstaller>());
        }

        [TestMethod]
        public void MsSqlDbInstaller_EmptyArgsIsInvalid()
        {
            var parameters = new MsSqlDatabaseInstallationOptions();
            var installer = CreateInstaller(parameters);
                
            Exception exception = null;

            // ACTION
            try
            {
                installer.ValidateParameters(parameters);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // ASSERT
            Assert.IsNotNull(exception);
            Assert.IsInstanceOfType(exception, typeof(ArgumentException));
            Assert.AreEqual("ExpectedDatabaseName cannot be null or empty.", exception.Message);
        }

        [TestMethod]
        public void MsSqlDbInstaller_CreatorUserWithoutPasswordIsInvalid()
        {
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = "server",
                DatabaseName = "DB1",
                DbCreatorUserName = "Creator1",
                DbCreatorPassword = null
            };
            var installer = CreateInstaller(parameters);
            Exception exception = null;

            // ACTION
            try
            {
                installer.GetConnectionString(parameters);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // ASSERT
            Assert.IsNotNull(exception);
            Assert.IsInstanceOfType(exception, typeof(ArgumentException));
            Assert.AreEqual("Value cannot be null. (Parameter 'Password')", exception.Message);
        }

        [TestMethod]
        public void MsSqlDbInstaller_ConnectionString_LocalServerInstance()
        {
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = LocalServer,
                DatabaseName = "DB1"
            };
            var installer = CreateInstaller(parameters);

            // ACTION
            var connectionString = installer.GetConnectionString(parameters);

            // ASSERT
            Assert.AreEqual($"Data Source={LocalServer};Initial Catalog=DB1;Integrated Security=True",
                connectionString);
        }

        [DataRow(null, null, null, "Data Source=(local);Initial Catalog=DB1;Integrated Security=True")]
        [DataRow(".\\Instance", null, null, "Data Source=.\\Instance;Initial Catalog=DB1;Integrated Security=True")]
        [DataRow(null, "U1", "P1", "Data Source=(local);Initial Catalog=DB1;User ID=U1;Password=P1")]
        [DataRow(null, null, "P1", "Data Source=(local);Initial Catalog=DB1;Integrated Security=True")]
        [DataTestMethod]
        public void MsSqlDbInstaller_ConnectionStrings(string server, string user, string password, string result)
        {
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = server,
                DatabaseName = "DB1",
                DbCreatorUserName = user,
                DbCreatorPassword = password
            };
            var installer = CreateInstaller(parameters);

            // ACTION
            var connectionString = installer.GetConnectionString(parameters);

            // ASSERT
            Assert.AreEqual(result, connectionString);
        }

        [TestMethod]
        public void MsSqlDbInstaller_CleanInstall_SqlCreatorForSqlCustomer()
        {
            CleanupServer("Database1", "Customer1");
            EnsureCreator("Creator1", "CreatorPassword1");

            // ACTION
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = LocalServer,
                DatabaseName = "Database1",
                DbCreatorUserName = "Creator1",
                DbCreatorPassword = "CreatorPassword1",
                DbOwnerUserName = "Customer1",
                DbOwnerPassword = "CustomerPassword1"
            };
            var installer = CreateInstaller(parameters);
            installer.InstallAsync().GetAwaiter().GetResult();

            // ASSERT
            Assert.IsTrue(IsDatabaseExists("Database1"));
            Assert.IsTrue(IsDbOwner("Creator1", "Database1") ||
                          IsDbOwner2("Creator1", "Database1"));
            Assert.IsTrue(IsDbOwner("Customer1", "Database1") ||
                          IsDbOwner2("Customer1", "Database1"));
            var status = GetInstallationStatus(parameters);
            AssertInstallationStatus(true, "Creator1", false, true, true, true,
                status);
        }

        [TestMethod]
        public void MsSqlDbInstaller_CleanInstall_SqlCreatorForHerself()
        {
            CleanupServer("Database1", "Customer1");
            EnsureCreator("Creator1", "CreatorPassword1");

            // ACTION
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = LocalServer,
                DatabaseName = "Database1",
                DbCreatorUserName = "Creator1",
                DbCreatorPassword = "CreatorPassword1",
                DbOwnerUserName = "Creator1",
                DbOwnerPassword = "CreatorPassword1"
            };
            var installer = CreateInstaller(parameters);
            installer.InstallAsync().GetAwaiter().GetResult();

            // ASSERT
            Assert.IsTrue(IsDatabaseExists("Database1"));
            var integratedUserName = $"{Environment.UserDomainName}\\{Environment.UserName}";
            Assert.IsFalse(IsDbOwner(integratedUserName, "Database1") ||
                           IsDbOwner2(integratedUserName, "Database1"));
            Assert.IsTrue(IsDbOwner("Creator1", "Database1") ||
                          IsDbOwner2("Creator1", "Database1"));
            Assert.IsFalse(IsDbOwner("Customer1", "Database1") ||
                           IsDbOwner2("Customer1", "Database1"));
            var status = GetInstallationStatus(parameters);
            AssertInstallationStatus(true, "Creator1", true, false, true, true,
                status);
        }

        [TestMethod, TestCategory("Services")]
        public void MsSqlDbInstaller_CleanInstall_IntegratedCreatorForHerself_CSrv()
        {
            CleanupServer("Database1", "Customer1");
            EnsureCreator("Creator1", "CreatorPassword1");

            // ACTION
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = LocalServer,
                DatabaseName = "Database1",
                DbCreatorUserName = null,
                DbCreatorPassword = null,
                DbOwnerUserName = null,
                DbOwnerPassword = null
            };
            var installer = CreateInstaller(parameters);
            installer.InstallAsync().GetAwaiter().GetResult();

            // ASSERT
            Assert.IsTrue(IsDatabaseExists("Database1"));
            var integratedUserName = $"{Environment.UserDomainName}\\{Environment.UserName}";
            Assert.IsTrue(IsDbOwner(integratedUserName, "Database1") ||
                          IsDbOwner2(integratedUserName, "Database1"));
            Assert.IsFalse(IsDbOwner("Creator1", "Database1") ||
                           IsDbOwner2("Creator1", "Database1"));
            Assert.IsFalse(IsDbOwner("Customer1", "Database1") ||
                           IsDbOwner2("Customer1", "Database1"));
            var status = GetInstallationStatus(parameters);
            var integratedUser = $"{Environment.UserDomainName}\\{Environment.UserName}";
            AssertInstallationStatus(true, integratedUser, true, false, null, null,
                status);
        }

        [TestMethod]
        public void MsSqlDbInstaller_CleanInstall_IntegratedCreatorForSqlCustomer()
        {
            CleanupServer("Database1", "Customer1");
            EnsureCreator("Creator1", "CreatorPassword1");

            // ACTION
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = LocalServer,
                DatabaseName = "Database1",
                DbCreatorUserName = null,
                DbCreatorPassword = null,
                DbOwnerUserName = "Customer1",
                DbOwnerPassword = "CustomerPassword1"
            };
            var installer = CreateInstaller(parameters);
            installer.InstallAsync().GetAwaiter().GetResult();

            // ASSERT
            Assert.IsTrue(IsDatabaseExists("Database1"));
            var integratedUserName = $"{Environment.UserDomainName}\\{Environment.UserName}";
            Assert.IsTrue(IsDbOwner(integratedUserName, "Database1") ||
                          IsDbOwner2(integratedUserName, "Database1"));
            Assert.IsFalse(IsDbOwner("Creator1", "Database1") ||
                           IsDbOwner2("Creator1", "Database1"));
            Assert.IsTrue(IsDbOwner("Customer1", "Database1") ||
                          IsDbOwner2("Customer1", "Database1"));
            var status = GetInstallationStatus(parameters);
            var integratedUser = $"{Environment.UserDomainName}\\{Environment.UserName}";
            AssertInstallationStatus(true, integratedUser, false, true, null, true,
                status);
        }

        [TestMethod]
        public void MsSqlDbInstaller_PartialInstall_ExistingCustomer()
        {
            var database = "Database1";
            var userName = "Customer1";
            var password = "CustomerPassword1";
            CleanupServer(database, userName);
            EnsureLogin(userName, password);
            Assert.IsTrue(IsLoginExists(userName));

            // ACTION
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = LocalServer,
                DatabaseName = database,
                DbCreatorUserName = null,
                DbCreatorPassword = null,
                DbOwnerUserName = userName,
                DbOwnerPassword = password
            };
            var installer = CreateInstaller(parameters);
            installer.InstallAsync().GetAwaiter().GetResult();

            // ASSERT
            Assert.IsTrue(IsDatabaseExists(database));
            var integratedUserName = $"{Environment.UserDomainName}\\{Environment.UserName}";
            Assert.IsTrue(IsDbOwner(integratedUserName, database) ||
                          IsDbOwner2(integratedUserName, database));
            Assert.IsFalse(IsDbOwner("Creator1", database) ||
                           IsDbOwner2("Creator1", database));
            Assert.IsTrue(IsDbOwner(userName, database) ||
                          IsDbOwner2(userName, database));
        }

        [TestMethod]
        public void MsSqlDbInstaller_PartialInstall_ExistingCustomer_Disabled()
        {
            var database = "Database1";
            var userName = "Customer1";
            var password = "CustomerPassword1";
            CleanupServer(database, userName);
            EnsureLogin(userName, password);
            DisableLogin(userName);
            Assert.IsTrue(IsLoginExists(userName));

            // ACTION
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = LocalServer,
                DatabaseName = database,
                DbCreatorUserName = null,
                DbCreatorPassword = null,
                DbOwnerUserName = userName,
                DbOwnerPassword = password
            };
            var installer = CreateInstaller(parameters);
            Exception exception = null;
            try
            {
                installer.InstallAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                exception = e;
            }

            // ASSERT
            Assert.IsFalse(IsDatabaseExists(database));
            Assert.IsNotNull(exception);
            Assert.IsInstanceOfType(exception, typeof(DbCreationException));
            Assert.AreEqual("User is disabled on this server.", exception.Message);
        }

        [TestMethod]
        public void MsSqlDbInstaller_PartialInstall_ExistingCustomer_Force()
        {
            var database = "Database1";
            var userName = "Customer1";
            var password = "CustomerPassword1";
            CleanupServer(database, userName);
            EnsureLogin(userName, password);
            Assert.IsTrue(IsLoginExists(userName));

            // ACTION 2nd creation is enabled: inner exception 15025 is caught.
            var installer = CreateInstaller();
            installer.CreateLoginAsync(userName, password, SystemConnectionString).ConfigureAwait(false).GetAwaiter()
                .GetResult();

            // ASSERT
            // There is no any exception
        }

        [TestMethod]
        public void MsSqlDbInstaller_PartialInstall_ExistingDatabase()
        {
            var database = "Database1";
            var userName = "Customer1";
            var password = "CustomerPassword1";
            CleanupServer(database, userName);
            var installer = CreateInstaller();
            installer.CreateDatabaseAsync(database, SystemConnectionString).ConfigureAwait(false).GetAwaiter()
                .GetResult();
            Assert.IsTrue(IsDatabaseExists(database));

            // ACTION
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = LocalServer,
                DatabaseName = database,
                DbCreatorUserName = null,
                DbCreatorPassword = null,
                DbOwnerUserName = userName,
                DbOwnerPassword = password
            };
            installer = CreateInstaller(parameters);
            installer.InstallAsync().GetAwaiter().GetResult();

            // ASSERT
            Assert.IsTrue(IsDatabaseExists(database));
            var integratedUserName = $"{Environment.UserDomainName}\\{Environment.UserName}";
            Assert.IsTrue(IsDbOwner(integratedUserName, database) ||
                          IsDbOwner2(integratedUserName, database));
            Assert.IsTrue(IsDbOwner(userName, database) ||
                          IsDbOwner2(userName, database));
        }

        [TestMethod]
        public void MsSqlDbInstaller_PartialInstall_ExistingDatabase_Force()
        {
            var database = "Database1";
            CleanupServer(database);
            var installer = CreateInstaller();
            installer.CreateDatabaseAsync(database, SystemConnectionString).ConfigureAwait(false).GetAwaiter()
                .GetResult();
            Assert.IsTrue(IsDatabaseExists(database));

            // ACTION: 2nd creation is enabled: inner exception 1801 is caught.
            installer.CreateDatabaseAsync(database, SystemConnectionString).ConfigureAwait(false).GetAwaiter()
                .GetResult();

            // ASSERT
            // There is no any exception
        }

        [TestMethod]
        public void MsSqlDbInstaller_PartialInstall_ExistingUserButNotOwner()
        {
            var database = "Database1";
            var userName = "Customer1";
            var password = "CustomerPassword1";
            CleanupServer(database, userName);
            EnsureLogin(userName, password);
            var installer = CreateInstaller();
            installer.CreateDatabaseAsync(database, SystemConnectionString).ConfigureAwait(false).GetAwaiter()
                .GetResult();
            installer.CreateUserAsync(userName, GetConnectionStringFor(database)).ConfigureAwait(false).GetAwaiter()
                .GetResult();
            Assert.IsTrue(IsLoginExists(userName));
            Assert.IsTrue(IsDatabaseExists(database));

            // ACTION
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = LocalServer,
                DatabaseName = database,
                DbCreatorUserName = null,
                DbCreatorPassword = null,
                DbOwnerUserName = userName,
                DbOwnerPassword = password
            };
            installer = CreateInstaller(parameters);
            installer.InstallAsync().GetAwaiter().GetResult();

            // ASSERT
            Assert.IsTrue(IsDatabaseExists(database));
            var integratedUserName = $"{Environment.UserDomainName}\\{Environment.UserName}";
            Assert.IsTrue(IsDbOwner(integratedUserName, database) ||
                          IsDbOwner2(integratedUserName, database));
            Assert.IsTrue(IsDbOwner(userName, database) ||
                          IsDbOwner2(userName, database));
        }

        [TestMethod]
        public void MsSqlDbInstaller_PartialInstall_ExistingUserButNotOwner_Force()
        {
            var database = "Database1";
            var userName = "Customer1";
            var password = "CustomerPassword1";
            var connectionString = GetConnectionStringFor(database);
            CleanupServer(database, userName);
            EnsureLogin(userName, password);
            var installer = CreateInstaller();
            installer.CreateDatabaseAsync(database, SystemConnectionString).ConfigureAwait(false).GetAwaiter()
                .GetResult();
            installer.CreateUserAsync(userName, connectionString).ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.IsTrue(IsLoginExists(userName));
            Assert.IsTrue(IsDatabaseExists(database));

            // ACTION 2nd creation is enabled: inner exception 15023 is caught.
            installer.CreateUserAsync(userName, connectionString).ConfigureAwait(false).GetAwaiter().GetResult();

            // ASSERT
            // There is no any exception
        }

        [TestMethod]
        public void MsSqlDbInstaller_PartialInstall_SecondInstall()
        {
            var database = "Database1";
            var userName = "Customer1";
            var password = "CustomerPassword1";
            CleanupServer(database, userName);
            EnsureLogin(userName, password);
            var connectionString = GetConnectionStringFor(database);
            var installer = CreateInstaller();
            installer.CreateDatabaseAsync(database, SystemConnectionString).ConfigureAwait(false).GetAwaiter()
                .GetResult();
            installer.CreateUserAsync(userName, connectionString).ConfigureAwait(false).GetAwaiter().GetResult();
            installer.AddDbOwnerRoleAsync(userName, connectionString).ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.IsTrue(IsLoginExists(userName));
            Assert.IsTrue(IsDatabaseExists(database));
            Assert.IsTrue(IsDbOwner(userName, database));

            // ACTION
            var parameters = new MsSqlDatabaseInstallationOptions
            {
                Server = LocalServer,
                DatabaseName = database,
                DbCreatorUserName = null,
                DbCreatorPassword = null,
                DbOwnerUserName = userName,
                DbOwnerPassword = password
            };
            installer = CreateInstaller(parameters);
            installer.InstallAsync().GetAwaiter().GetResult();

            // ASSERT
            Assert.IsTrue(IsDatabaseExists(database));
            var integratedUserName = $"{Environment.UserDomainName}\\{Environment.UserName}";
            Assert.IsTrue(IsDbOwner(integratedUserName, database) ||
                          IsDbOwner2(integratedUserName, database));
            Assert.IsTrue(IsDbOwner(userName, database) ||
                          IsDbOwner2(userName, database));
        }

        [TestMethod]
        public void MsSqlDbInstaller_FailedInstall_CreateLogin()
        {
            var database = "Database1";
            CleanupServer(database);
            var creatorName = "Creator2";
            var creatorPassword = "CreatorPassword2";
            EnsureRestrictiveCreator(creatorName, creatorPassword);
            var userName = "Customer2";
            var password = "CustomerPassword2";
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = LocalServer,
                InitialCatalog = "master",
                UserID = creatorName,
                Password = creatorPassword
            };

            // ACTION
            Exception exception = null;
            try
            {
                var installer = CreateInstaller();
                installer.CreateLoginAsync(userName, password, builder.ConnectionString).ConfigureAwait(false)
                    .GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                exception = e;
            }

            // ASSERT
            Assert.IsFalse(IsLoginExists(userName));
            Assert.IsNotNull(exception);
            Assert.IsInstanceOfType(exception, typeof(DbCreationException));
            Assert.IsTrue(exception.InnerException is SqlException);
            Assert.AreEqual($"Cannot create user on the server. {exception.InnerException.Message}",
                exception.Message); // User does not have permission to perform this action..
        }

        [TestMethod]
        public void MsSqlDbInstaller_FailedInstall_CreateDatabase()
        {
            var database = "Database1";
            var userName = "Customer1";
            var password = "CustomerPassword1";
            CleanupServer(database, userName);
            var creatorName = "Creator2";
            var creatorPassword = "CreatorPassword2";
            EnsureRestrictiveCreator(creatorName, creatorPassword);
            CreateLogin(userName, password);

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = LocalServer,
                InitialCatalog = "master",
                UserID = creatorName,
                Password = creatorPassword
            };

            // ACTION
            Exception exception = null;
            try
            {
                var installer = CreateInstaller();
                installer.CreateDatabaseAsync(database, builder.ConnectionString).ConfigureAwait(false).GetAwaiter()
                    .GetResult();
            }
            catch (Exception e)
            {
                exception = e;
            }

            // ASSERT
            Assert.IsFalse(IsDatabaseExists(database));
            Assert.IsNotNull(exception);
            Assert.IsInstanceOfType(exception, typeof(DbCreationException));
            Assert.IsTrue(exception.InnerException is SqlException);
            Assert.AreEqual($"Cannot create database. {exception.InnerException.Message}",
                exception.Message); // CREATE DATABASE permission denied in database 'master'.
        }

        /* ===================================================================================== */

        private static readonly string SystemConnectionString =
            $"Data Source={LocalServer};Initial Catalog=master;Integrated Security=True";

        private string GetConnectionStringFor(string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(SystemConnectionString);
            builder.InitialCatalog = databaseName;
            return builder.ConnectionString;
        }

        private void CleanupServer(string databaseName, string userName)
        {
            if (IsDatabaseExists(databaseName))
                DropUser(databaseName, userName);
            DropDatabase(databaseName);
            DropLogin(userName);
            Assert.IsFalse(IsDatabaseExists(databaseName));
            Assert.IsFalse(IsLoginExists(userName));
        }

        private void CleanupServer(string databaseName)
        {
            DropDatabase(databaseName);
            Assert.IsFalse(IsDatabaseExists(databaseName));
        }

        private void DropUser(string databaseName, string userName)
        {
            ExecuteCommand(GetConnectionStringFor(databaseName), $"DROP USER IF EXISTS [{userName}]");
        }

        private void DropDatabase(string databaseName)
        {
            ExecuteCommand(SystemConnectionString,
                @$"IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
BEGIN
    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE 
    DROP DATABASE [{databaseName}]
END 
");
        }

        private void DropLogin(string loginName)
        {
            var sessions = new List<int>();
            var sql = "SELECT session_id FROM sys.dm_exec_sessions WHERE login_name = 'Customer1'";
            ExecuteSqlQuery(sql, SystemConnectionString, reader =>
            {
                sessions.Add(reader.GetInt16(reader.GetOrdinal("session_id")));
                return true;
            });

            ExecuteCommand(SystemConnectionString,
                $"IF EXISTS (SELECT * FROM sys.server_principals WHERE name = N'{loginName}') DROP LOGIN [{loginName}]");
        }

        private bool IsDatabaseExists(string databaseName)
        {
            var sql = $"SELECT [name] FROM sys.databases WHERE name = N'{databaseName}'";
            var isExist = false;
            ExecuteSqlQuery(sql, SystemConnectionString, reader =>
            {
                isExist = true;
                return false;
            });
            return isExist;
        }

        private bool IsDbOwner(string userName, string databaseName)
        {
            var sql =
                @$"SELECT roles.name role, members.name member FROM sys.database_role_members AS database_role_members  
JOIN sys.database_principals AS roles ON database_role_members.role_principal_id = roles.principal_id  
JOIN sys.database_principals AS members ON database_role_members.member_principal_id = members.principal_id
WHERE members.name = '{userName}' AND roles.name = 'db_owner'";
            var isExist = false;
            ExecuteSqlQuery(sql, GetConnectionStringFor(databaseName), reader =>
            {
                isExist = true;
                return false;
            });
            return isExist;
        }

        private bool IsDbOwner2(string userName, string databaseName)
        {
            var sql =
                @$"SELECT suser_sname(owner_sid) DbOwner/*, [name] DbName*/ FROM sys.databases WHERE [name] = '{databaseName}'";
            var result = false;
            ExecuteSqlQuery(sql, SystemConnectionString, reader =>
            {
                var name = reader.GetString(reader.GetOrdinal("DbOwner"));
                result = name == userName;
                return false;
            });
            return result;
        }

        private void EnsureCreator(string userName, string password)
        {
            EnsureLogin(userName, password);
            ExecuteCommand(SystemConnectionString, $"ALTER LOGIN {userName} ENABLE");
            ExecuteCommand(SystemConnectionString, $"EXEC sp_addsrvrolemember {userName}, dbcreator");
            ExecuteCommand(SystemConnectionString, $"EXEC sp_addsrvrolemember {userName}, securityadmin");
            ExecuteCommand(SystemConnectionString, $"GRANT ALTER ANY DATABASE TO {userName}");
        }

        private void EnsureRestrictiveCreator(string userName, string password)
        {
            EnsureLogin(userName, password);
            ExecuteCommand(SystemConnectionString, $"ALTER LOGIN {userName} ENABLE");
        }

        private void EnsureLogin(string userName, string password)
        {
            if (!IsLoginExists(userName))
                CreateLogin(userName, password);
        }

        private bool IsLoginExists(string userName)
        {
            var sql =
                $"SELECT [name], [type], [type_desc], [is_disabled] FROM sys.server_principals WHERE [name] = '{userName}'";
            var isExist = false;
            ExecuteSqlQuery(sql, SystemConnectionString, reader =>
            {
                isExist = true;
                return false;
            });
            return isExist;
        }

        private static void CreateLogin(string userName, string password)
        {
            ExecuteCommand(SystemConnectionString,
                $"CREATE LOGIN {userName} WITH PASSWORD=N'{password}', DEFAULT_DATABASE = MASTER, DEFAULT_LANGUAGE = US_ENGLISH");
        }

        private void DisableLogin(string userName)
        {
            ExecuteCommand(SystemConnectionString, $"ALTER LOGIN {userName} DISABLE");
        }

        private MsSqlDbInstallationStatus GetInstallationStatus(MsSqlDatabaseInstallationOptions options)
        {
            var dbCreatorUserName = options.DbCreatorUserName == null ? "null" : $"'{options.DbCreatorUserName}'";
            var dbOwnerUserName = options.DbOwnerUserName == null ? "null" : $"'{options.DbOwnerUserName}'";
            var sql = @$"declare @database nvarchar(450) set @database = '{options.DatabaseName}'
declare @creatorLogin nvarchar(450) set @creatorLogin = {dbCreatorUserName} -- should be null if the creator is an integrated user
declare @customerLogin nvarchar(450) set @customerLogin = {dbOwnerUserName} -- should be null if the customer is an integrated user
declare @integratedUser nvarchar(450) set @integratedUser = '{Environment.UserDomainName}\{Environment.UserName}' -- should given if the @customerLogin is null

DECLARE @databaseExists bit SET @databaseExists = 0
DECLARE @dbOwnerOk bit SET @dbOwnerOk = 0
DECLARE @dbOwnerRoleOk bit SET @dbOwnerRoleOk = 0
DECLARE @creatorLoginExists bit SET @creatorLoginExists = 0
DECLARE @creatorLoginDisabled bit
DECLARE @customerLoginExists bit SET @customerLoginExists = 0
DECLARE @customerLoginDisabled bit

IF EXISTS (SELECT [name] FROM sys.databases WHERE [name] = @database)
    SET @databaseExists = 1

SELECT @creatorLoginDisabled = [is_disabled] FROM sys.server_principals WHERE [name] = @creatorLogin
IF @creatorLoginDisabled IS NOT NULL
	SET @creatorLoginExists = 1

SELECT @customerLoginDisabled = [is_disabled] FROM sys.server_principals WHERE [name] = @customerLogin
IF @customerLoginDisabled IS NOT NULL
	SET @customerLoginExists = 1

IF EXISTS
	(SELECT roles.name role, members.name member FROM sys.database_role_members AS database_role_members  
		JOIN sys.database_principals AS roles ON database_role_members.role_principal_id = roles.principal_id  
		JOIN sys.database_principals AS members ON database_role_members.member_principal_id = members.principal_id
	WHERE members.name = @customerLogin AND roles.name = 'db_owner')
	SET @dbOwnerRoleOk = 1

DECLARE @expectedDbOwner nvarchar(450)
SELECT @expectedDbOwner = CASE WHEN @customerLogin IS NOT NULL THEN @customerLogin ELSE @integratedUser END

DECLARE @dbOwner nvarchar(450)
SELECT @dbOwner = suser_sname(owner_sid) FROM sys.databases WHERE [name] = @database

IF @dbOwner = @expectedDbOwner
	SET @dbOwnerOk = 1

SELECT @databaseExists databaseExists
	, @dbOwner dbOwner
	, @dbOwnerOk dbOwnerOk
	, @dbOwnerRoleOk dbOwnerRoleOk
	, @creatorLoginExists creatorLoginExists
	, CAST(1 - @creatorLoginDisabled AS bit) creatorLoginEnabled
	, @customerLoginExists customerLoginExists
	, CAST(1 - @customerLoginDisabled AS bit) customerLoginEnabled
";
            var result = new MsSqlDbInstallationStatus();
            ExecuteSqlQuery(sql, GetConnectionStringFor(options.DatabaseName), reader =>
            {
                result.IsDatabaseExist = reader.GetSafeBooleanFromBoolean("databaseExists");
                result.DbOwner = reader.GetSafeString("dbOwner");
                result.DbOwnerOk = reader.GetSafeBooleanFromBoolean("dbOwnerOk");
                result.DbOwnerRoleOk = reader.GetSafeBooleanFromBoolean("dbOwnerRoleOk");
                result.CreatorLoginExists = reader.GetSafeBooleanFromBoolean("creatorLoginExists");
                result.CreatorLoginEnabled = reader.GetSafeBooleanFromBoolean("creatorLoginEnabled");
                result.CustomerLoginExists = reader.GetSafeBooleanFromBoolean("customerLoginExists");
                result.CustomerLoginEnabled = reader.GetSafeBooleanFromBoolean("customerLoginEnabled");
                return false;
            });
            return result;
        }
        private void AssertInstallationStatus(bool dbExist, string dbOwner, bool dbOwnerOk, bool dbOwnerRoleOk,
            bool? creatorEnabled, bool? customerEnabled, MsSqlDbInstallationStatus status)
        {
            Assert.AreEqual(dbExist, status.IsDatabaseExist, $"IsDatabaseExist is {status.IsDatabaseExist}, expected: {dbExist}.");
            Assert.AreEqual(dbOwner, status.DbOwner, $"DbOwner is {status.DbOwner}, expected: {dbOwner}.");
            Assert.AreEqual(dbOwnerOk, status.DbOwnerOk, $"DbOwnerOk is {status.DbOwnerOk}, expected: {dbOwnerOk}.");
            Assert.AreEqual(dbOwnerRoleOk, status.DbOwnerRoleOk, $"DbOwnerRoleOk is {status.DbOwnerRoleOk}, expected: {dbOwnerRoleOk}.");
            if (creatorEnabled == null)
            {
                Assert.AreEqual(false, status.CreatorLoginExists, $"CreatorLoginExists is {status.CreatorLoginExists}, expected: {false}.");
                Assert.AreEqual(false, status.CreatorLoginEnabled, $"CreatorLoginEnabled is {status.CreatorLoginEnabled}, expected: {false}.");
            }
            else
            {
                Assert.AreEqual(true, status.CreatorLoginExists, $"CreatorLoginExists is {status.CreatorLoginExists}, expected: {true}.");
                Assert.AreEqual(creatorEnabled.Value, status.CreatorLoginEnabled, $"CreatorLoginEnabled is {status.CreatorLoginEnabled}, expected: {creatorEnabled.Value}.");
            }
            if (customerEnabled == null)
            {
                Assert.AreEqual(false, status.CustomerLoginExists, $"CustomerLoginExists is {status.CustomerLoginExists}, expected: {false}.");
                Assert.AreEqual(false, status.CustomerLoginEnabled, $"CustomerLoginEnabled is {status.CustomerLoginEnabled}, expected: {false}.");
            }
            else
            {
                Assert.AreEqual(true, status.CustomerLoginExists, $"CustomerLoginExists is {status.CustomerLoginExists}, expected: {true}.");
                Assert.AreEqual(customerEnabled.Value, status.CustomerLoginEnabled, $"CustomerLoginEnabled is {status.CustomerLoginEnabled}, expected: {customerEnabled.Value}.");
            }
        }

        private static void ExecuteCommand(string connectionString, string sql)
        {
            using (var cn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.CommandType = CommandType.Text;
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        private void ExecuteSqlQuery(string sql, string connectionString, Func<SqlDataReader, bool> processRow)
        {
            using (var cn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.CommandType = CommandType.Text;
                cn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read() && processRow(reader)) ;
            }
        }
    }
}
