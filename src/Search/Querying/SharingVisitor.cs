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

            // Return if there is no any sharing related ckause.
            if (ReferenceEquals(scanned, predicate))
                return predicate;

            // Return simple rewritten sharing predicate.
            if (!(scanned is LogicalPredicate))
                return scanned;

            // Handle logical predicates
            var normalizedClauses = scanner.TopLevelSharingClauses
                .Select(x => new SharingNormalizerVisitor().VisitLogicalClause(x));
            var normalizedSharingPredicate = new LogicalPredicate(normalizedClauses);

            // Make combinations
            var composer = new SharingComposerVisitor();
            var composition = (LogicalPredicate)composer.Visit(normalizedSharingPredicate);

            // Convert sharing combined values from string array to one comma separated string
            var finalizer = new SharingFinalizerVisitor();
            var finalTree = (LogicalPredicate)finalizer.Visit(composition);

            // Return the final product
            var allClauses = scanner.TopLevelGeneralClauses.Union(finalTree.Clauses);
            return new LogicalPredicate(allClauses);
        }
    }
}
