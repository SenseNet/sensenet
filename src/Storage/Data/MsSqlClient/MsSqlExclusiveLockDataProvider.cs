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

private string ____sql____ = null;
        /// <inheritdoc/>
        public async Task<bool> AcquireAsync(ExclusiveBlockContext context, string key, DateTime timeLimit, CancellationToken cancellationToken)
        {
            using (var ctx = MainProvider.CreateDataContext(cancellationToken))
            {
                var result = await ctx.ExecuteScalarAsync(____sql____, cmd =>
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
                await ctx.ExecuteNonQueryAsync(____sql____, cmd =>
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
                await ctx.ExecuteNonQueryAsync(____sql____, cmd =>
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
                var result = await ctx.ExecuteScalarAsync(____sql____, cmd =>
                {
                    cmd.Parameters.AddRange(new[]
                    {
                        ctx.CreateParameter("@LockName", DbType.String, key)
                    });
                }).ConfigureAwait(false);

                return result != DBNull.Value;
            }
        }
    }
}
