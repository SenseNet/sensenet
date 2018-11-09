using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    internal class SharingScannerVisitor : SnQueryVisitor //UNDONE:<? Move to ContentRepository (?)
    {
        private LogicalPredicate _topLevelLogicalPredicate;

        public List<LogicalClause> TopLevelGeneralClauses { get; } = new List<LogicalClause>();
        public List<LogicalClause> TopLevelSharingClauses { get; } = new List<LogicalClause>();

        private static readonly string[] SharingRelatedFieldNames =
            {SharingVisitor.Sharing, "SharedWith", "SharedBy", "SharingMode", "SharingLevel" };

        private readonly Stack<bool> _isSharingStack = new Stack<bool>();

        public override SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            if (_topLevelLogicalPredicate == null)
                if (IsFinished(predicate))
                    return predicate;
            return base.Visit(predicate);
        }
        private bool IsFinished(SnQueryPredicate topLevelPredicate)
        {
            _topLevelLogicalPredicate = topLevelPredicate as LogicalPredicate;
            return _topLevelLogicalPredicate == null;
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
            throw new NotSupportedException(); //UNDONE:<? exception message: sharing range
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
                    throw new NotSupportedException(); //UNDONE:<? exception message: must not of sharing
                if (i == 0 || isTopLevel)
                    continue;
                if (isSharing[0] != isSharing[i])
                    throw new NotSupportedException(); //UNDONE:<? exception message: inhomogeneous
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
