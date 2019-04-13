using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Newtonsoft.Json;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    public class SqlPackagingDataProvider : IPackagingDataProviderExtension
    {
        private DataProvider _mainProvider;
        public DataProvider MainProvider => _mainProvider ?? (_mainProvider = DataProvider.Instance);

        #region SQL InstalledComponentsScript
        private static readonly string InstalledComponentsScript = @"SELECT P2.Description, P1.ComponentId, P1.ComponentVersion, P1a.ComponentVersion AcceptableVersion
FROM (SELECT ComponentId, MAX(ComponentVersion) ComponentVersion FROM Packages WHERE ComponentId IS NOT NULL GROUP BY ComponentId) P1
JOIN (SELECT ComponentId, MAX(ComponentVersion) ComponentVersion FROM Packages WHERE ComponentId IS NOT NULL 
    AND ExecutionResult != '" + ExecutionResult.Faulty.ToString() + @"'
    AND ExecutionResult != '" + ExecutionResult.Unfinished.ToString() + @"' GROUP BY ComponentId, ExecutionResult) P1a
ON P1.ComponentId = P1a.ComponentId
JOIN (SELECT Description, ComponentId FROM Packages WHERE PackageType = '" + PackageType.Install.ToString() + @"'
    AND ExecutionResult != '" + ExecutionResult.Faulty.ToString() + @"'
    AND ExecutionResult != '" + ExecutionResult.Unfinished.ToString() + @"') P2
ON P1.ComponentId = P2.ComponentId";
        #endregion
        public IEnumerable<ComponentInfo> LoadInstalledComponents()
        {
            var components = new List<ComponentInfo>();
            using (var cmd = MainProvider.CreateDataProcedure(InstalledComponentsScript))
            {
                cmd.CommandType = CommandType.Text;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        components.Add(new ComponentInfo
                        {
                            ComponentId = reader.GetSafeString(reader.GetOrdinal("ComponentId")),                                         // varchar  50   null
                            Version = DecodePackageVersion(reader.GetSafeString(reader.GetOrdinal("ComponentVersion"))),                  // varchar  50   null
                            AcceptableVersion = DecodePackageVersion(reader.GetSafeString(reader.GetOrdinal("AcceptableVersion"))), // varchar  50   null
                            Description = reader.GetSafeString(reader.GetOrdinal("Description")),                                   // nvarchar 1000 null
                        });
                    }
                }
            }
            return components;
        }
        public IEnumerable<Package> LoadInstalledPackages()
        {
            var packages = new List<Package>();
            using (var cmd = MainProvider.CreateDataProcedure("SELECT * FROM Packages"))
            {
                cmd.CommandType = CommandType.Text;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        packages.Add(new Package
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),                                                                                  // int           not null
                            Description = reader.GetSafeString(reader.GetOrdinal("Description")),                                                           // nvarchar 1000 null
                            ComponentId = reader.GetSafeString(reader.GetOrdinal("ComponentId")),                                                                       // varchar 50    null
                            PackageType = (PackageType)Enum.Parse(typeof(PackageType), reader.GetString(reader.GetOrdinal("PackageType"))),             // varchar 50    not null
                            ReleaseDate = reader.GetDateTimeUtc(reader.GetOrdinal("ReleaseDate")),                                                             // datetime      not null
                            ExecutionDate = reader.GetDateTimeUtc(reader.GetOrdinal("ExecutionDate")),                                                         // datetime      not null
                            ExecutionResult = (ExecutionResult)Enum.Parse(typeof(ExecutionResult), reader.GetString(reader.GetOrdinal("ExecutionResult"))), // varchar 50    not null
                            ExecutionError = DeserializeExecutionError(reader.GetSafeString(reader.GetOrdinal("ExecutionError"))),
                            ComponentVersion = DecodePackageVersion(reader.GetSafeString(reader.GetOrdinal("ComponentVersion"))),                                                                      // varchar 50    null
                        });
                    }
                }
            }
            return packages;
        }
        private Version GetSafeVersion(SqlDataReader reader, string columnName)
        {
            var version = reader.GetSafeString(reader.GetOrdinal(columnName));
            if (version == null)
                return null;
            return Version.Parse(version);
        }

        #region SQL SavePackageScript
        private static readonly string SavePackageScript = @"INSERT INTO Packages
    (  Description,  ComponentId,  PackageType,  ReleaseDate,  ExecutionDate,  ExecutionResult,  ExecutionError,  ComponentVersion,  Manifest) VALUES
    ( @Description, @ComponentId, @PackageType, @ReleaseDate, @ExecutionDate, @ExecutionResult, @ExecutionError, @ComponentVersion, @Manifest)
SELECT @@IDENTITY";
        #endregion
        public void SavePackage(Package package)
        {
            using (var cmd = MainProvider.CreateDataProcedure(SavePackageScript))
            {
                cmd.CommandType = CommandType.Text;

                AddParameter(cmd, "@Description", SqlDbType.NVarChar, 1000).Value = (object)package.Description ?? DBNull.Value;
                AddParameter(cmd, "@ComponentId", SqlDbType.VarChar, 50).Value = (object)package.ComponentId ?? DBNull.Value;
                AddParameter(cmd, "@PackageType", SqlDbType.VarChar, 50).Value = package.PackageType.ToString();
                AddParameter(cmd, "@ReleaseDate", SqlDbType.DateTime).Value = package.ReleaseDate;
                AddParameter(cmd, "@ExecutionDate", SqlDbType.DateTime).Value = package.ExecutionDate;
                AddParameter(cmd, "@ExecutionResult", SqlDbType.VarChar, 50).Value = package.ExecutionResult.ToString();
                AddParameter(cmd, "@ExecutionError", SqlDbType.NVarChar).Value = SerializeExecutionError(package.ExecutionError) ?? (object)DBNull.Value;
                AddParameter(cmd, "@ComponentVersion", SqlDbType.VarChar, 50).Value = package.ComponentVersion == null ? DBNull.Value : (object)EncodePackageVersion(package.ComponentVersion);
                AddParameter(cmd, "@Manifest", SqlDbType.NVarChar).Value = package.Manifest ?? (object)DBNull.Value;

                var result = cmd.ExecuteScalar();
                package.Id = Convert.ToInt32(result);
            }
        }

        #region SQL UpdatePackageScript
        private static readonly string UpdatePackageScript = @"UPDATE Packages
    SET ComponentId = @ComponentId,
        Description = @Description,
        PackageType = @PackageType,
        ReleaseDate = @ReleaseDate,
        ExecutionDate = @ExecutionDate,
        ExecutionResult = @ExecutionResult,
        ExecutionError = @ExecutionError,
        ComponentVersion = @ComponentVersion
WHERE Id = @Id
";
        #endregion
        public void UpdatePackage(Package package)
        {
            using (var cmd = MainProvider.CreateDataProcedure(UpdatePackageScript))
            {
                cmd.CommandType = CommandType.Text;

                AddParameter(cmd, "@Id", SqlDbType.Int).Value = package.Id;
                AddParameter(cmd, "@Description", SqlDbType.NVarChar, 1000).Value = (object)package.Description ?? DBNull.Value;
                AddParameter(cmd, "@ComponentId", SqlDbType.VarChar, 50).Value = (object)package.ComponentId ?? DBNull.Value;
                AddParameter(cmd, "@PackageType", SqlDbType.VarChar, 50).Value = package.PackageType.ToString();
                AddParameter(cmd, "@ReleaseDate", SqlDbType.DateTime).Value = package.ReleaseDate;
                AddParameter(cmd, "@ExecutionDate", SqlDbType.DateTime).Value = package.ExecutionDate;
                AddParameter(cmd, "@ExecutionResult", SqlDbType.VarChar, 50).Value = package.ExecutionResult.ToString();
                AddParameter(cmd, "@ExecutionError", SqlDbType.NVarChar).Value = SerializeExecutionError(package.ExecutionError) ?? (object)DBNull.Value;
                AddParameter(cmd, "@ComponentVersion", SqlDbType.VarChar, 50).Value = package.ComponentVersion == null ? DBNull.Value : (object)EncodePackageVersion(package.ComponentVersion);

                cmd.ExecuteNonQuery();
            }
        }

        #region SQL PackageExistenceScript
        private static readonly string PackageExistenceScript = @"SELECT COUNT(0) FROM Packages
WHERE ComponentId = @ComponentId AND PackageType = @PackageType AND ComponentVersion = @Version
";
        #endregion
        public bool IsPackageExist(string componentId, PackageType packageType, Version version)
        {
            int count;
            using (var cmd = MainProvider.CreateDataProcedure(PackageExistenceScript))
            {
                cmd.CommandType = CommandType.Text;

                AddParameter(cmd, "@ComponentId", SqlDbType.VarChar, 50).Value = (object)componentId ?? DBNull.Value;
                AddParameter(cmd, "@PackageType", SqlDbType.VarChar, 50).Value = packageType.ToString();
                AddParameter(cmd, "@Version", SqlDbType.VarChar, 50).Value = EncodePackageVersion(version);
                count = (int)cmd.ExecuteScalar();
            }
            return count > 0;
        }
        
        public void DeletePackage(Package package)
        {
            if (package.Id < 1)
                throw new ApplicationException("Cannot delete unsaved package");

            using (var cmd = MainProvider.CreateDataProcedure("DELETE FROM Packages WHERE Id = @Id"))
            {
                cmd.CommandType = CommandType.Text;

                AddParameter(cmd, "@Id", SqlDbType.Int).Value = package.Id;
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteAllPackages()
        {
            using (var cmd = MainProvider.CreateDataProcedure("TRUNCATE TABLE Packages"))
            {
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        #region SQL LoadManifestScript
        private static readonly string LoadManifestScript = @"SELECT Manifest FROM Packages WHERE Id = @Id";
        #endregion
        public void LoadManifest(Package package)
        {
            using (var cmd = MainProvider.CreateDataProcedure(LoadManifestScript))
            {
                cmd.CommandType = CommandType.Text;

                AddParameter(cmd, "@Id", SqlDbType.Int).Value = package.Id;

                var value = cmd.ExecuteScalar();
                package.Manifest = (string)(value == DBNull.Value ? null : value);
            }
        }

        /* ---------------------------------------------- */

        private IDataParameter AddParameter(IDataProcedure proc, string name, SqlDbType dbType)
        {
            var p = new SqlParameter(name, dbType);
            proc.Parameters.Add(p);
            return p;
        }
        private IDataParameter AddParameter(IDataProcedure proc, string name, SqlDbType dbType, int size)
        {
            var p = new SqlParameter(name, dbType, size);
            proc.Parameters.Add(p);
            return p;
        }

        private static string EncodePackageVersion(Version v)
        {
            if (v.Build < 0)
                return $"{v.Major:0#########}.{v.Minor:0#########}";
            if (v.Revision < 0)
                return $"{v.Major:0#########}.{v.Minor:0#########}.{v.Build:0#########}";
            return $"{v.Major:0#########}.{v.Minor:0#########}.{v.Build:0#########}.{v.Revision:0#########}";
        }
        private static Version DecodePackageVersion(string s)
        {
            return s == null ? null : Version.Parse(s);
        }

        private string SerializeExecutionError(Exception e)
        {
            if (e == null)
                return null;

            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            try
            {
                using (var sw = new StringWriter())
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                        serializer.Serialize(writer, e);
                    return sw.GetStringBuilder().ToString();
                }
            }
            catch (Exception ee)
            {
                using (var sw = new StringWriter())
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                        serializer.Serialize(writer, new Exception("Cannot serialize the execution error: " + ee.Message));
                    return sw.GetStringBuilder().ToString();
                }
            }
        }
        private Exception DeserializeExecutionError(string data)
        {
            if (data == null)
                return null;

            var serializer = new JsonSerializer();
            using (var jreader = new JsonTextReader(new StringReader(data)))
                return serializer.Deserialize<Exception>(jreader);
        }
    }
}
