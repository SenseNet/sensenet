using System.Collections.Generic;
using SenseNet.Search.Parser;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.ContentRepository.Linq
{
    internal class OptimizeBooleansVisitor : SnQueryVisitor
    {
        public override SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            var boolMemberPredicate = predicate as SnLinqVisitor.BooleanMemberPredicate;
            if (boolMemberPredicate != null)
                return VisitBooleanMemberQuery(boolMemberPredicate);
            return base.Visit(predicate);
        }

        private SnQueryPredicate VisitBooleanMemberQuery(SnLinqVisitor.BooleanMemberPredicate boolMemberQ)
        {
            return SnLinqVisitor.CreateTextPredicate(boolMemberQ.FieldName, boolMemberQ.Value);
        }

        public override SnQueryPredicate VisitLogicalPredicate(LogicalPredicate logic)
        {
            var clauses = logic.Clauses;
            var visitedClauses = VisitLogicalClauses(clauses);

            var newClauses = new List<LogicalClause>();
            var changed = false;
            foreach (var clause in visitedClauses)
            {
                var lp = clause.Predicate as LogicalPredicate;
                if (lp == null)
                {
                    newClauses.Add(clause);
                }
                else
                {
                    var subClauses = lp.Clauses;
                    var must = 0;
                    var mustnot = 0;
                    var should = 0;
                    Occurence occur;
                    for (var i = 0; i < subClauses.Count; i++)
                    {
                        occur = subClauses[i].Occur;
                        if (occur == Occurence.Must) must++;
                        else if (occur == Occurence.MustNot) mustnot++;
                        else if (occur == Occurence.Should) should++;
                        else if (occur == Occurence.Default) should++;
                    }
                    occur = clause.Occur;

                    if (mustnot == 0 && should == 0) // MUST
                    {
                        if (occur == Occurence.Must)
                            AddClause(newClauses, subClauses, Occurence.Must);
                        else if (occur == Occurence.MustNot)
                            AddClause(newClauses, subClauses, Occurence.MustNot);
                        else
                            newClauses.Add(clause);
                        changed = true;
                    }
                    else if (must == 0 && should == 0) // MUST_NOT
                    {
                        if (occur == Occurence.Must)
                            AddClause(newClauses, subClauses, Occurence.MustNot);
                        else if (occur == Occurence.MustNot)
                            AddClause(newClauses, subClauses, Occurence.Must);
                        else
                            newClauses.Add(clause);
                        changed = true;
                    }
                    else // not changed
                    {
                        newClauses.Add(clause);
                    }
                }
            }
            if (changed)
                visitedClauses = newClauses;

            LogicalPredicate newQuery = null;
            if (visitedClauses != clauses)
                newQuery = new LogicalPredicate(visitedClauses);
            return newQuery ?? logic;
        }

        private void AddClause(ICollection<LogicalClause> newClauses, IEnumerable<LogicalClause> oldClauses, Occurence newOccur)
        {
            foreach (var oldClause in oldClauses)
            {
                oldClause.Occur = newOccur;
                newClauses.Add(oldClause);
            }
        }

    }
}
