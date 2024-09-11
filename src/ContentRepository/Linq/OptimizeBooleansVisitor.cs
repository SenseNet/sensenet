using System.Collections.Generic;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.ContentRepository.Linq
{
    internal class OptimizeBooleansVisitor : SnQueryVisitor
    {
        public override SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            if (predicate is SnLinqVisitor.BooleanMemberPredicate boolMemberPredicate)
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
                if (!(clause.Predicate is LogicalPredicate lp))
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

                    if (mustnot == 0 && should == 0) // Only MUST subclauses
                    {
                        if (occur == Occurence.Must) // +(+T1 +T2) --> +T1 +T2
                        {
                            AddClause(newClauses, subClauses, Occurence.Must);
                            changed = true;
                        }
                        else if (occur == Occurence.MustNot) // -(+T1 +T2) --> no change
                            newClauses.Add(clause);
                        else // _(+T1 +T2) --> no change
                            newClauses.Add(clause);
                    }
                    else if (must == 0 && should == 0) // Only MUST_NOT subclauses
                    {
                        if (occur == Occurence.Must) // +(-T1 -T2) --> -T1 -T2
                        {
                            AddClause(newClauses, subClauses, Occurence.MustNot);
                            changed = true;
                        }
                        else if (occur == Occurence.MustNot) // +(-T1 -T2) --> no change
                            newClauses.Add(clause);
                        else // _(-T1 -T2) --> no change
                            newClauses.Add(clause);
                    }
                    else if (must == 0 && mustnot == 0) // Only SHOULD subclauses
                    {
                        if (occur == Occurence.Should) // _(T1 T2) --> T1 T2
                        {
                            AddClause(newClauses, subClauses, Occurence.Should);
                            changed = true;
                        }
                        else if (occur == Occurence.MustNot) // -(T1 T2) --> -T1 -T2 (deMorgan)
                        {
                            AddClause(newClauses, subClauses, Occurence.MustNot);
                            changed = true;
                        }
                        else
                            newClauses.Add(clause);
                    }
                    else if (should == 0) // Only MUST and MUST_NOT subclauses
                    {
                        if (occur == Occurence.Must) // +(+T1 -T2) --> +T1 -T2
                        {
                            AddClause(newClauses, subClauses);
                            changed = true;
                        }
                        else if (occur == Occurence.MustNot) // -(+T1 -T2) --> no change
                            newClauses.Add(clause);
                        else // _(+T1 -T2) --> no change
                            newClauses.Add(clause);
                    }
                    else
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

        private void AddClause(ICollection<LogicalClause> newClauses, IEnumerable<LogicalClause> oldClauses, Occurence newOccur = Occurence.Default)
        {
            foreach (var oldClause in oldClauses)
            {
                if(newOccur != Occurence.Default)
                    oldClause.Occur = newOccur;
                newClauses.Add(oldClause);
            }
        }
    }
}
