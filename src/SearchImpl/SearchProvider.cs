using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;
using SenseNet.Search.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Search
{
    internal abstract class SearchProvider //UNDONE: move to Luc29 implementation
    {
        private static SearchProvider[] _providers = new[] { new SqlSearchProvider() };
        private static SearchProvider _fallbackProvider = new LuceneSearchProvider();

        public static SearchProvider FallBackProvider { get { return _fallbackProvider; } }

        internal static IQueryExecutor GetExecutor(LucQuery lucQuery)
        {
            var queryInfo = lucQuery.QueryInfo;

            var provider = GetProvider(lucQuery, queryInfo);
            IQueryExecutor executor = null;
            if (provider != null)
                executor = provider.GetExecutor();
            if (executor == null)
                executor = _fallbackProvider.CreateNew(queryInfo).GetExecutor();
            return executor;
        }
        internal static IQueryExecutor GetFallbackExecutor(LucQuery lucQuery)
        {
            var queryInfo = lucQuery.QueryInfo;
            return _fallbackProvider.CreateNew(queryInfo).GetExecutor();
        }

        private static SearchProvider GetProvider(LucQuery lucQuery, SnQueryInfo queryInfo)
        {
            SearchProvider candidate;
            foreach (var prototype in _providers)
            {
                candidate = prototype.CreateNew(queryInfo);
                if (candidate.CanExecute())
                {
                    candidate.QueryInfo = queryInfo;
                    return candidate;
                }
            }
            return null;
        }

        public SnQueryInfo QueryInfo { get; set; }

        public abstract SearchProvider CreateNew(SnQueryInfo queryInfo);
        public abstract bool CanExecute();
        public abstract IQueryExecutor GetExecutor();
    }


    internal class SqlSearchProvider : SearchProvider
    {
        public override SearchProvider CreateNew(SnQueryInfo queryInfo)
        {
            return new SqlSearchProvider() { QueryInfo = queryInfo };
        }
        public override bool CanExecute()
        {
            return SnLucToSqlCompiler.CanCompile(QueryInfo);
        }
        public override IQueryExecutor GetExecutor()
        {
            string _sqlQueryText;
            NodeQueryParameter[] _sqlParameters;

            if (SnLucToSqlCompiler.TryCompile(QueryInfo.Query.QueryTree, QueryInfo.Top, QueryInfo.Skip, QueryInfo.SortFields, QueryInfo.CountOnly, out _sqlQueryText, out _sqlParameters))
                return new SqlQueryExecutor(_sqlQueryText, _sqlParameters);
            return null;
        }
    }


    internal class LuceneSearchProvider : SearchProvider
    {
        public override SearchProvider CreateNew(SnQueryInfo queryInfo)
        {
            return new LuceneSearchProvider() { QueryInfo = queryInfo };
        }
        public override bool CanExecute()
        {
            return true;
        }
        public override IQueryExecutor GetExecutor()
        {
            if (QueryInfo.CountOnly)
                return new QueryExecutor20131012CountOnly();
            return new QueryExecutor20131012();
        }
    }

}
