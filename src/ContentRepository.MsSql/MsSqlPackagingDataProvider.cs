﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using STT=System.Threading.Tasks;
using Newtonsoft.Json;
using SenseNet.Diagnostics;

// ReSharper disable AccessToDisposedClosure

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary> 
    /// This is an MS SQL implementation of the <see cref="IPackagingDataProvider"/> interface.
    /// It requires the main data provider to be a <see cref="RelationalDataProviderBase"/>.
    /// </summary>
    public class MsSqlPackagingDataProvider : IPackagingDataProvider
    {
        private readonly RelationalDataProviderBase _mainProvider;

        public MsSqlPackagingDataProvider(DataProvider mainProvider)
        {
            if (mainProvider == null)
                return;
            if (!(mainProvider is RelationalDataProviderBase relationalDataProviderBase))
                throw new ArgumentException("The mainProvider need to be RelationalDataProviderBase.");
            _mainProvider = relationalDataProviderBase;
        }

        #region SQL LoadInstalledComponentsScript
        private static readonly string InstalledComponentsScript = $@"-- MsSqlPackagingDataProvider.LoadInstalledComponents
SELECT ComponentId, PackageType, ComponentVersion, Description, Manifest
FROM Packages WHERE
	(PackageType = '{PackageType.Install}' OR PackageType = '{PackageType.Patch}') AND
	ExecutionResult = '{ExecutionResult.Successful}' 
ORDER BY ComponentId, ComponentVersion, ExecutionDate
";
        #endregion
        public async STT.Task<IEnumerable<ComponentInfo>> LoadInstalledComponentsAsync(CancellationToken cancellationToken)
        {
            if (!(await _mainProvider.IsDatabaseReadyAsync(cancellationToken)))
                return new ComponentInfo[0];

            var components = new Dictionary<string, ComponentInfo>();
            var descriptions = new Dictionary<string, string>();

            using var op = SnTrace.Database.StartOperation("MsSqlPackagingDataProvider: LoadInstalledComponents()");
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
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

            foreach (var item in descriptions)
                components[item.Key].Description = item.Value;
            op.Successful = true;

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
        public async STT.Task<IEnumerable<ComponentInfo>> LoadIncompleteComponentsAsync(CancellationToken cancellationToken)
        {
            if (!(await _mainProvider.IsDatabaseReadyAsync(cancellationToken)))
                return new ComponentInfo[0];

            var components = new Dictionary<string, ComponentInfo>();
            var descriptions = new Dictionary<string, string>();

            using var op = SnTrace.Database.StartOperation("MsSqlPackagingDataProvider: LoadIncompleteComponents()");
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
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

            foreach (var item in descriptions)
                components[item.Key].Description = item.Value;
            op.Successful = true;

            return components.Values.ToArray();
        }

        public async STT.Task<IEnumerable<Package>> LoadInstalledPackagesAsync(CancellationToken cancellationToken)
        {
            var packages = new List<Package>();

            using var op = SnTrace.Database.StartOperation("MsSqlPackagingDataProvider: LoadInstalledPackages()");
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
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
            op.Successful = true;

            return packages;
        }

        #region SQL SavePackageScript
        private static readonly string SavePackageScript = @"INSERT INTO Packages
    (  Description,  ComponentId,  PackageType,  ReleaseDate,  ExecutionDate,  ExecutionResult,  ExecutionError,  ComponentVersion,  Manifest) VALUES
    ( @Description, @ComponentId, @PackageType, @ReleaseDate, @ExecutionDate, @ExecutionResult, @ExecutionError, @ComponentVersion, @Manifest)
SELECT @@IDENTITY";
        #endregion
        public async STT.Task SavePackageAsync(Package package, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlPackagingDataProvider: " +
                "SavePackage: ComponentId: {0}, ComponentVersion: {1}, ExecutionResult: {2}",
                package.ComponentId, package.ComponentVersion, package.ExecutionResult);

            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
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
            op.Successful = true;
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
        public async STT.Task UpdatePackageAsync(Package package, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlPackagingDataProvider: " +
                "UpdatePackage: ComponentId: {0}, ComponentVersion: {1}, ExecutionResult: {2}",
                package.ComponentId, package.ComponentVersion, package.ExecutionResult);

            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
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
            op.Successful = true;
        }

        #region SQL PackageExistenceScript
        private static readonly string PackageExistenceScript = @"SELECT COUNT(0) FROM Packages
WHERE ComponentId = @ComponentId AND PackageType = @PackageType AND ComponentVersion = @Version
";
        #endregion
        public async STT.Task<bool> IsPackageExistAsync(string componentId, PackageType packageType, Version version
            , CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlPackagingDataProvider: " +
                "IsPackageExist(componentId: {0}, packageType: {1}, version: {2})", componentId, packageType, version);
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteScalarAsync(PackageExistenceScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@ComponentId", DbType.AnsiString, 50, (object)componentId ?? DBNull.Value),
                    ctx.CreateParameter("@PackageType", DbType.AnsiString, 50, packageType.ToString()),
                    ctx.CreateParameter("@Version", DbType.AnsiString, 50, EncodePackageVersion(version))
                });
            }).ConfigureAwait(false);
            var count = (int)result;
            op.Successful = true;

            return count > 0;
        }
        
        public async STT.Task DeletePackageAsync(Package package, CancellationToken cancellationToken)
        {
            if (package.Id < 1)
                throw new ApplicationException("Cannot delete unsaved package");

            using var op = SnTrace.Database.StartOperation("MsSqlPackagingDataProvider: " +
                "DeletePackage: Id: {0}", package.Id);
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync("DELETE FROM Packages WHERE Id = @Id",
                cmd => { cmd.Parameters.Add(ctx.CreateParameter("@Id", DbType.Int32, package.Id)); }).ConfigureAwait(false);
            op.Successful = true;
        }

        public async STT.Task DeleteAllPackagesAsync(CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlPackagingDataProvider: DeleteAllPackages()");
            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            await ctx.ExecuteNonQueryAsync("TRUNCATE TABLE Packages").ConfigureAwait(false);
            op.Successful = true;
        }

        #region SQL LoadManifestScript
        private static readonly string LoadManifestScript = @"SELECT Manifest FROM Packages WHERE Id = @Id";
        #endregion
        public async STT.Task LoadManifestAsync(Package package, CancellationToken cancellationToken)
        {
            using var op = SnTrace.Database.StartOperation("MsSqlPackagingDataProvider: " +
                "LoadManifest: Id: {0}", package.Id);

            using var ctx = _mainProvider.CreateDataContext(cancellationToken);
            var result = await ctx.ExecuteScalarAsync(LoadManifestScript, cmd =>
            {
                cmd.Parameters.AddRange(new[]
                {
                    ctx.CreateParameter("@Id", DbType.Int32, package.Id)
                });
            }).ConfigureAwait(false);
            package.Manifest = (string)(result == DBNull.Value ? null : result);

            op.Successful = true;
        }

        /* =============================================================================== Methods for Steps */

        public Dictionary<string, string> GetContentPathsWhereTheyAreAllowedChildren(List<string> names)
        {
            var result = new Dictionary<string, string>();

            var whereClausePart = string.Join(Environment.NewLine + "    OR" + Environment.NewLine,
                names.Select(n =>
                    $"    (t.Value like '{n}' OR t.Value like '% {n} %' OR t.Value like '{n} %' OR t.Value like '% {n}')"));

            // testability: the first line is recognizable for the tests.
            var sql = $"-- GetContentPathsWhereTheyAreAllowedChildren: [{string.Join(", ", names)}]" +
                      Environment.NewLine;
            sql += @"SELECT n.Path, t.Value FROM LongTextProperties t
	JOIN PropertyTypes p ON p.PropertyTypeId = t.PropertyTypeId
	JOIN Versions v ON t.VersionId = v.VersionId
	JOIN Nodes n ON n.NodeId = v.NodeId
WHERE p.Name = 'AllowedChildTypes' AND (
" + whereClausePart + @"
)
";
            using var op = SnTrace.Database.StartOperation("MsSqlPackagingDataProvider:" +
                " GetContentPathsWhereTheyAreAllowedChildren()");

            //TODO: [DIREF] get options from DI through constructor
            using var ctx = _mainProvider.CreateDataContext(CancellationToken.None);
            var _ = ctx.ExecuteReaderAsync(sql, async (reader, cancel) =>
            {
                cancel.ThrowIfCancellationRequested();
                while (await reader.ReadAsync(cancel).ConfigureAwait(false))
                {
                    cancel.ThrowIfCancellationRequested();
                    result.Add(reader.GetString(0), reader.GetString(1));
                }
                return STT.Task.FromResult(0);
            }).GetAwaiter().GetResult();
            op.Successful = true;

            return result;
        }

        /* =============================================================================== TOOLS */

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

            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            try
            {
                using var sw = new StringWriter();
                using (JsonWriter writer = new JsonTextWriter(sw))
                    serializer.Serialize(writer, e);
                return sw.GetStringBuilder().ToString();
            }
            catch (Exception ee)
            {
                using var sw = new StringWriter();
                using (JsonWriter writer = new JsonTextWriter(sw))
                    serializer.Serialize(writer, new Exception("Cannot serialize the execution error: " + ee.Message));
                return sw.GetStringBuilder().ToString();
            }
        }
        private Exception DeserializeExecutionError(string data)
        {
            if (data == null)
                return null;

            var serializer = new JsonSerializer();
            using var jreader = new JsonTextReader(new StringReader(data));
            return serializer.Deserialize<Exception>(jreader);
        }
    }
}
