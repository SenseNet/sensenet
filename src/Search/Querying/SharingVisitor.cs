using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    internal class SharingVisitor : SnQueryVisitor //UNDONE:<? Move to ContentRepository (?)
    {
        internal static readonly string Sharing = "Sharing";

        public override SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            var scanner = new SharingScannerVisitor();
            var scanned = scanner.Visit(predicate);

            if (object.ReferenceEquals(scanned, predicate))
                return predicate;

            var normalizedClauses = scanner.TopLevelSharingClauses
                .Select(x => new SharingNormalizerVisitor().VisitLogicalClause(x));
            var normalizedSharingPredicate = new LogicalPredicate(normalizedClauses);

            var composer = new SharingComposerVisitor();
            var composition = (LogicalPredicate)composer.Visit(normalizedSharingPredicate); //UNDONE:<? can be simple predicate?

            var finalizer = new SharingFinalizerVisitor();
            var finalTree = (LogicalPredicate)finalizer.Visit(composition);


            var allClauses = scanner.TopLevelGeneralClauses.Union(finalTree.Clauses);
            return new LogicalPredicate(allClauses);
        }
    }
}
