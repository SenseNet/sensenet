using System.Linq;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    internal class SharingVisitor : SnQueryVisitor //UNDONE:<? Move to ContentRepository (?)
    {
        internal static readonly string Sharing = "Sharing";

        public override SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            // Categorice clauses and prepare sharing clauses.
            var scanner = new SharingScannerVisitor();
            var scanned = scanner.Visit(predicate);
            return scanned;
        }
    }
}
