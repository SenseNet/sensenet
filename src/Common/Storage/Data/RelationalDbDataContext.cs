using System.Data.Common;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data
{
    public class RelationalDbDataContext : SnDctx<DbConnection, DbCommand, DbParameter, DbDataReader>
    {
        private readonly IDataPlatform<DbConnection, DbCommand, DbParameter> _dataPlatform;

        public RelationalDbDataContext(IDataPlatform<DbConnection, DbCommand, DbParameter> dataPlatform,
            CancellationToken cancellationToken = default(CancellationToken))
            : base(cancellationToken)
        {
            _dataPlatform = dataPlatform;
        }

        public override DbConnection CreateConnection()
        {
            return _dataPlatform.CreateConnection();
        }
        public override DbCommand CreateCommand()
        {
            return _dataPlatform.CreateCommand();
        }
        public override DbParameter CreateParameter()
        {
            return _dataPlatform.CreateParameter();
        }
    }
}
