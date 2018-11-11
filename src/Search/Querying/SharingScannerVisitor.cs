using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    internal class SharingScannerVisitor : SnQueryVisitor //UNDONE:<? Move to ContentRepository (?)
    {
        private bool _initialized;
        private LogicalPredicate _topLevelLogicalPredicate;

        public List<LogicalClause> TopLevelGeneralClauses { get; } = new List<LogicalClause>();
        public List<LogicalClause> TopLevelSharingClauses { get; } = new List<LogicalClause>();

        private static readonly string[] SharingRelatedFieldNames =
            {SharingVisitor.Sharing, "SharedWith", "SharedBy", "SharingMode", "SharingLevel" };

        private readonly Stack<bool> _isSharingStack = new Stack<bool>();

        public override SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            if (!_initialized)
            {
                _topLevelLogicalPredicate = predicate as LogicalPredicate;
                _initialized = true;
            }
            return base.Visit(predicate);
        }

        public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
        {
            if (!SharingRelatedFieldNames.Contains(simplePredicate.FieldName))
            {
                _isSharingStack.Push(false);
                return simplePredicate;
            }
            _isSharingStack.Push(true);
            return SharingComposerVisitor.CreateSharingSimplePredicate(simplePredicate.Value);
        }
        public override SnQueryPredicate VisitRangePredicate(RangePredicate range)
        {
            if (!SharingRelatedFieldNames.Contains(range.FieldName))
            {
                _isSharingStack.Push(false);
                return range;
            }
            throw new InvalidContentSharingQueryException("Range query cannot be used in a sharing related query clause.");
        }

        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            var isTopLevel = _topLevelLogicalPredicate == logic;

            var visited = (LogicalPredicate)base.VisitLogicalPredicate(logic);
            var clauseCount = visited.Clauses.Count;

            // Get all sub-item tyőes in righr order
            var isSharing = new bool[clauseCount];
            for (var i = 0; i < clauseCount; i++)
                isSharing[clauseCount - i - 1] = _isSharingStack.Pop();

            for (var i = 0; i < clauseCount; i++)
            {
                if (isSharing[i] && visited.Clauses[i].Occur == Occurence.MustNot)
                    throw new InvalidContentSharingQueryException("Sharing related query clause cannot be negation.");
                if (i == 0 || isTopLevel)
                    continue;
                if (isSharing[0] != isSharing[i])
                    throw new InvalidContentSharingQueryException("One parenthesis level should not contain sharing related and not-sharing related clauses.");
            }

            if (isTopLevel)
            {
                for (int i = 0; i < clauseCount; i++)
                {
                    if(isSharing[i])
                        TopLevelSharingClauses.Add(visited.Clauses[i]);
                    else
                        TopLevelGeneralClauses.Add(visited.Clauses[i]);
                }
            }
            else
            {
                _isSharingStack.Push(isSharing[0]);
            }
            return visited;
        }
    }
}
