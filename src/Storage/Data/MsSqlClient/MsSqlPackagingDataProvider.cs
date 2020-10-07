using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
// ReSharper disable AccessToDisposedClosure

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary> 
    /// This is an MS SQL implementation of the <see cref="IPackagingDataProviderExtension"/> interface.
    /// It requires the main data provider to be a <see cref="RelationalDataProviderBase"/>.
    /// </summary>
    public class MsSqlPackagingDataProvider : IPackagingDataProviderExtension
    {
        private RelationalDataProviderBase _dataProvider;
        private RelationalDataProviderBase MainProvider => _dataProvider ?? (_dataProvider = (RelationalDataProviderBase)DataStore.DataProvider);

        #region SQL LoadInstalledComponentsScript
        private static readonly string InstalledComponentsScript = $@"-- MsSqlPackagingDataProvider.LoadInstalledComponents
SELECT ComponentId, PackageType, ComponentVersion, Description, Manifest
FROM Packages WHERE
	(PackageType = '{PackageType.Install}' OR PackageType = '{PackageType.Patch}') AND
	ExecutionResult = '{ExecutionResult.Successful}' 
ORDER BY ComponentId, ComponentVersion, ExecutionDate
";
        #endregion
        public async Task<IEnumerable<ComponentInfo>> LoadInstalledComponentsAsync(CancellationToken cancellationToken)
        {
            var components = new Dictionary<string, ComponentInfo>();
            var descriptions = new Dictionary<string, string>();

            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteReaderAsync(InstalledComponentsScript,
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        {
                            cancel.ThrowIfCancellationRequested();

                            var component = new ComponentInfo
                            {
                                ComponentId = reader.GetSafeString(reader.GetOrdinal("ComponentId")),
                                Version = DecodePackageVersion(
                                    reader.GetSafeString(reader.GetOrdinal("ComponentVersion"))),
                                Description = reader.GetSafeString(reader.GetOrdinal("Description")),
                                Manifest = reader.GetSafeString(reader.GetOrdinal("Manifest")),
                                ExecutionResult = ExecutionResult.Successful
                            };

                            components[component.ComponentId] = component;
                            if (reader.GetSafeString(reader.GetOrdinal("PackageType"))
                                == nameof(PackageType.Install))
                                descriptions[component.ComponentId] = component.Description;
                        }

                        return true;
                    }).ConfigureAwait(false);
            }

            foreach (var item in descriptions)
                components[item.Key].Description = item.Value;

            return components.Values.ToArray();
        }

        #region SQL LoadIncompleteComponentsScript
        private static readonly string IncompleteComponentsScript = $@"-- MsSqlPackagingDataProvider.LoadIncompleteComponents
SELECT ComponentId, PackageType, ComponentVersion, Description, Manifest, ExecutionResult
FROM Packages WHERE
	(PackageType = '{PackageType.Install}' OR PackageType = '{PackageType.Patch}') AND
	ExecutionResult != '{ExecutionResult.Successful}' 
ORDER BY ComponentId, ComponentVersion, ExecutionDate
";
        #endregion
        public async Task<IEnumerable<ComponentInfo>> LoadIncompleteComponentsAsync(CancellationToken cancellationToken)
        {
            var components = new Dictionary<string, ComponentInfo>();
            var descriptions = new Dictionary<string, string>();

            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteReaderAsync(IncompleteComponentsScript,
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        {
                            cancel.ThrowIfCancellationRequested();

                            var src = reader.GetSafeString(reader.GetOrdinal("ExecutionResult"));
                            var executionResult = src == null
                                ? ExecutionResult.Unfinished
                                : (ExecutionResult) Enum.Parse(typeof(ExecutionResult), src);

                            var component = new ComponentInfo
                            {
                                ComponentId = reader.GetSafeString(reader.GetOrdinal("ComponentId")),
                                Version = DecodePackageVersion(
                                    reader.GetSafeString(reader.GetOrdinal("ComponentVersion"))),
                                Description = reader.GetSafeString(reader.GetOrdinal("Description")),
                                Manifest = reader.GetSafeString(reader.GetOrdinal("Manifest")),
                                ExecutionResult = executionResult
                            };

                            components[component.ComponentId] = component;
                            if (reader.GetSafeString(reader.GetOrdinal("PackageType"))
                                == nameof(PackageType.Install))
                                descriptions[component.ComponentId] = component.Description;
                        }

                        return true;
                    }).ConfigureAwait(false);
            }

            foreach (var item in descriptions)
                components[item.Key].Description = item.Value;

            return components.Values.ToArray();
        }

        public async Task<IEnumerable<Package>> LoadInstalledPackagesAsync(CancellationToken cancellationToken)
        {
            var packages = new List<Package>();

            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteReaderAsync("SELECT * FROM Packages",
                    async (reader, cancel) =>
                    {
                        cancel.ThrowIfCancellationRequested();
                        while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                        {
                            cancel.ThrowIfCancellationRequested();
                            packages.Add(new Package
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Description = reader.GetSafeString(reader.GetOrdinal("Description")),
                                ComponentId = reader.GetSafeString(reader.GetOrdinal("ComponentId")),
                                PackageType = (PackageType) Enum.Parse(typeof(PackageType),
                                    reader.GetString(reader.GetOrdinal("PackageType"))),
                                ReleaseDate = reader.GetDateTimeUtc(reader.GetOrdinal("ReleaseDate")),
                                ExecutionDate = reader.GetDateTimeUtc(reader.GetOrdinal("ExecutionDate")),
                                ExecutionResult = (ExecutionResult) Enum.Parse(typeof(ExecutionResult),
                                    reader.GetString(reader.GetOrdinal("ExecutionResult"))),
                                ExecutionError =
                                    DeserializeExecutionError(
                                        reader.GetSafeString(reader.GetOrdinal("ExecutionError"))),
                                ComponentVersion =
                                    DecodePackageVersion(reader.GetSafeString(reader.GetOrdinal("ComponentVersion"))),
                                Manifest = reader.GetSafeString(reader.GetOrdinal("Manifest")),
                            });
                        }

                        return true;
                    }).ConfigureAwait(false);
            }
            
            return packages;
        }

        #region SQL SavePackageScript
        private static readonly string SavePackageScript = @"INSERT INTO Packages
    (  Description,  ComponentId,  PackageType,  ReleaseDate,  ExecutionDate,  ExecutionResult,  ExecutionError,  ComponentVersion,  Manifest) VALUES
    ( @Description, @ComponentId, @PackageType, @ReleaseDate, @ExecutionDate, @ExecutionResult, @ExecutionError, @ComponentVersion, @Manifest)
SELECT @@IDENTITY";
        #endregion
        public async Task SavePackageAsync(Package package, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(SavePackageScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Description", DbType.String, 1000,
                            (object) package.Description ?? DBNull.Value),
                        ctx.CreateParameter("@ComponentId", DbType.AnsiString, 50,
                            (object) package.ComponentId ?? DBNull.Value),
                        ctx.CreateParameter("@PackageType", DbType.AnsiString, 50, package.PackageType.ToString()),
                        ctx.CreateParameter("@ReleaseDate", DbType.DateTime2, package.ReleaseDate),
                        ctx.CreateParameter("@ExecutionDate", DbType.DateTime2, package.ExecutionDate),
                        ctx.CreateParameter("@ExecutionResult", DbType.AnsiString, 50, package.ExecutionResult.ToString()),
                        ctx.CreateParameter("@ExecutionError", DbType.String, int.MaxValue,
                            SerializeExecutionError(package.ExecutionError) ?? (object) DBNull.Value),
                        ctx.CreateParameter("@ComponentVersion", DbType.AnsiString, 50,
                            package.ComponentVersion == null
                                ? DBNull.Value
                                : (object) EncodePackageVersion(package.ComponentVersion)),
                        ctx.CreateParameter("@Manifest", DbType.String, int.MaxValue, package.Manifest ?? (object) DBNull.Value)
                    });
                }).ConfigureAwait(false);

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
        public async Task UpdatePackageAsync(Package package, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(UpdatePackageScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Id", DbType.Int32, package.Id),
                        ctx.CreateParameter("@Description", DbType.String, 1000,
                            (object) package.Description ?? DBNull.Value),
                        ctx.CreateParameter("@ComponentId", DbType.AnsiString, 50,
                            (object) package.ComponentId ?? DBNull.Value),
                        ctx.CreateParameter("@PackageType", DbType.AnsiString, 50, package.PackageType.ToString()),
                        ctx.CreateParameter("@ReleaseDate", DbType.DateTime2, package.ReleaseDate),
                        ctx.CreateParameter("@ExecutionDate", DbType.DateTime2, package.ExecutionDate),
                        ctx.CreateParameter("@ExecutionResult", DbType.AnsiString, 50,
                            package.ExecutionResult.ToString()),
                        ctx.CreateParameter("@ExecutionError", DbType.String, int.MaxValue,
                            SerializeExecutionError(package.ExecutionError) ?? (object) DBNull.Value),
                        ctx.CreateParameter("@ComponentVersion", DbType.AnsiString, 50,
                            package.ComponentVersion == null
                                ? DBNull.Value
                                : (object) EncodePackageVersion(package.ComponentVersion))
                    });
                }).ConfigureAwait(false);
            }
        }

        #region SQL PackageExistenceScript
        private static readonly string PackageExistenceScript = @"SELECT COUNT(0) FROM Packages
WHERE ComponentId = @ComponentId AND PackageType = @PackageType AND ComponentVersion = @Version
";
        #endregion
        public async Task<bool> IsPackageExistAsync(string componentId, PackageType packageType, Version version
            , CancellationToken cancellationToken)
        {
            int count;
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(PackageExistenceScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@ComponentId", DbType.AnsiString, 50, (object)componentId ?? DBNull.Value),
                        ctx.CreateParameter("@PackageType", DbType.AnsiString, 50, packageType.ToString()),
                        ctx.CreateParameter("@Version", DbType.AnsiString, 50, EncodePackageVersion(version))
                    });
                }).ConfigureAwait(false);

                count = (int)result;
            }

            return count > 0;
        }
        
        public async Task DeletePackageAsync(Package package, CancellationToken cancellationToken)
        {
            if (package.Id < 1)
                throw new ApplicationException("Cannot delete unsaved package");

            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("DELETE FROM Packages WHERE Id = @Id",
                    cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Id", DbType.Int32, package.Id)); }).ConfigureAwait(false);
            }
        }

        public async Task DeleteAllPackagesAsync(CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("TRUNCATE TABLE Packages").ConfigureAwait(false);
            }
        }

        #region SQL LoadManifestScript
        private static readonly string LoadManifestScript = @"SELECT Manifest FROM Packages WHERE Id = @Id";
        #endregion
        public async Task LoadManifestAsync(Package package, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(LoadManifestScript, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@Id", DbType.Int32, package.Id)
                    });
                }).ConfigureAwait(false);

                package.Manifest = (string)(result == DBNull.Value ? null : result);
            }
        }

        /* ---------------------------------------------- */

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
