using System.Collections.Generic;
using System.Linq;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Main sharing visitor that encapsulates all sharing visitors. This type 
    /// is set as one of the visitor extensions when the repository starts.
    /// </summary>
    internal class SharingVisitor : SnQueryVisitor
    {
        internal static readonly string Sharing = "Sharing";

        private LogicalPredicate _topLevelLogicalPredicate;
        private bool _initialized;
        private bool _isSharinglDisabled;

        public List<LogicalClause> TopLevelGeneralClauses { get; } = new List<LogicalClause>();
        public List<LogicalClause> TopLevelSharingClauses { get; } = new List<LogicalClause>();

        private static readonly string[] SharingRelatedFieldNames =
            {Sharing, "SharedWith", "SharedBy", "SharingMode", "SharingLevel" };

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

        public override LogicalClause VisitLogicalClause(LogicalClause clause)
        {
            var visited = base.VisitLogicalClause(clause);
            if (_isSharingStack.Peek() && visited.Occur == Occurence.MustNot)
                throw new InvalidContentSharingQueryException("Sharing related query clause cannot be negation.");
            return visited;
        }

        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            var isTopLevel = _topLevelLogicalPredicate == logic;

            var visited = (LogicalPredicate)base.VisitLogicalPredicate(logic);
            var clauseCount = visited.Clauses.Count;

            // Get all sub-item types in right order
            var isSharingFlags = new bool[clauseCount];
            for (var i = 0; i < clauseCount; i++)
                isSharingFlags[clauseCount - i - 1] = _isSharingStack.Pop();

            var isSharing = isSharingFlags.Any(x => x);
            var isMixed = isSharingFlags.Any(x => x == !isSharingFlags[0]);
            if (isSharing && _isSharinglDisabled)
                throw new InvalidContentSharingQueryException("Only one parenthesis can contain sharing related and not-sharing related clauses.");

            if (!isMixed && !(isSharing && isTopLevel))
            {
                _isSharingStack.Push(isSharingFlags[0]);
                return visited;
            }

            var visitedSharing = VisitTopLevelSharingPredicate(visited, isSharingFlags);
            _isSharinglDisabled = true;
            // push false to avoid "mixed" rank in any higher level
            _isSharingStack.Push(false);

            return visitedSharing;
        }

        private LogicalPredicate VisitTopLevelSharingPredicate(LogicalPredicate logic, bool[] isSharingFlags)
        {
            var generalClauses = new List<LogicalClause>();
            var sharingClauses = new List<LogicalClause>();
            for (int i = 0; i < logic.Clauses.Count; i++)
            {
                if (isSharingFlags[i])
                    sharingClauses.Add(logic.Clauses[i]);
                else
                    generalClauses.Add(logic.Clauses[i]);
            }

            // ---------------------------------------------------


            // Handle logical predicates
            var sharingPredicate = new LogicalPredicate(sharingClauses);
            var normalizer = new SharingNormalizerVisitor();
            var normalizedSharingPredicate = normalizer.Visit(sharingPredicate);

            // Make combinations
            var composer = new SharingComposerVisitor();
            var composition = (LogicalPredicate)composer.Visit(normalizedSharingPredicate);

            // Last normalization
            var normalizedComposition = new SharingNormalizerVisitor().Visit(composition);

            // Convert sharing combined values from string array to one comma separated string
            var finalizer = new SharingFinalizerVisitor();
            var finalTree = (LogicalPredicate)finalizer.Visit(normalizedComposition);

            // Return the final product
            var allClauses = generalClauses.Union(finalTree.Clauses);
            return new LogicalPredicate(allClauses);
        }
    }
}
