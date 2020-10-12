using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable ConvertToUsingDeclaration

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.MsSqlClient
{
    /// <summary> 
    /// This is an MS SQL implementation of the <see cref="IExclusiveLockDataProviderExtension"/> interface.
    /// It requires the main data provider to be a <see cref="RelationalDataProviderBase"/>.
    /// </summary>
    public class MsSqlExclusiveLockDataProvider : IExclusiveLockDataProviderExtension
    {
        private RelationalDataProviderBase _dataProvider;
        private RelationalDataProviderBase MainProvider => _dataProvider ??= (_dataProvider = (RelationalDataProviderBase)DataStore.DataProvider);

        /// <inheritdoc/>
        public async Task<bool> AcquireAsync(ExclusiveBlockContext context, string key, DateTime timeLimit, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var sql = @"-- MsSqlExclusiveLockDataProvider.Acquire
BEGIN TRAN
DELETE FROM ExclusiveLocks WHERE [Name] = @name AND [TimeLimit] < @timeLimit
IF NOT EXISTS (SELECT Id FROM ExclusiveLocks WHERE [Name] = @name)
    INSERT INTO ExclusiveLocks ([Name], [TimeLimit])
        OUTPUT INSERTED.Id
        VALUES (@name, @timeLimit)
COMMIT
";
                var result = await ctx.ExecuteScalarAsync(sql, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@LockName", DbType.String, key)
                    });
                }).ConfigureAwait(false);

                return result != DBNull.Value;
            }
        }

        /// <inheritdoc/>
        public async Task RefreshAsync(string key, DateTime newTimeLimit, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync(
                    "UPDATE ExclusiveLocks SET [TimeLimit] = @timeLimit WHERE [Name] = @name",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@LockName", DbType.String, key),
                            ctx.CreateParameter("@TimeLimit", DbType.DateTime2, key)
                        });
                    }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task ReleaseAsync(string key, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                await ctx.ExecuteNonQueryAsync("DELETE FROM ExclusiveLocks WHERE @name = [Name]",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@LockName", DbType.String, key)
                        });
                    }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsLockedAsync(string key, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync("SELECT Id FROM ExclusiveLocks WHERE @name = [Name]",
                    cmd =>
                    {
                        cmd.Parameters.AddRange(new[]
                        {
                            ctx.CreateParameter("@LockName", DbType.String, key)
                        });
                    }).ConfigureAwait(false);

                return result != DBNull.Value;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsFeatureAvailable(CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(
                    "IF OBJECT_ID('ExclusiveLocks', 'U') IS NOT NULL SELECT 1 ELSE SELECT 0")
                    .ConfigureAwait(false);

                return 1 == (result == DBNull.Value ? 0 : (int)result);
            }
        }
    }
}
