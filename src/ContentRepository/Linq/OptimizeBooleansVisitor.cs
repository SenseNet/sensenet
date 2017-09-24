using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using SenseNet.Search.Parser;

namespace SenseNet.ContentRepository.Linq
{
    internal class OptimizeBooleansVisitor_NEW : SnQueryVisitor //UNDONE:. LINQ: OptimizeBooleansVisitor_NEW is not implemented
    {
        public override SnQueryPredicate Visit(SnQueryPredicate predicate)
        {
            return predicate;
        }
    }

    [Obsolete(", true")]
    internal class OptimizeBooleansVisitor : SenseNet.Search.Parser.LucQueryVisitor
    {
        public override Query Visit(Query q)
        {
            var boolMemberQ = q as CQVisitor.BooleanMemberQuery;
            if (boolMemberQ != null)
                return VisitBooleanMemberQuery(boolMemberQ);
            return base.Visit(q);
        }

        private Query VisitBooleanMemberQuery(CQVisitor.BooleanMemberQuery boolMemberQ)
        {
            return CQVisitor.CreateTermQuery(boolMemberQ.FieldName, boolMemberQ.Value);
        }
        public override Query VisitBooleanQuery(BooleanQuery booleanq)
        {
            var clauses = booleanq.GetClauses();
            var visitedClauses = VisitBooleanClauses(clauses);

            var newClauses = new List<BooleanClause>();
            var changed = false;
            foreach (var clause in visitedClauses)
            {
                var bq = clause.GetQuery() as BooleanQuery;
                if (bq == null)
                {
                    newClauses.Add(clause);
                    continue;
                }
                else
                {
                    var subClauses = bq.GetClauses();
                    var must = 0;
                    var mustnot = 0;
                    var should = 0;
                    BooleanClause.Occur occur;
                    for (int i = 0; i < subClauses.Length; i++)
                    {
                        occur = subClauses[i].GetOccur();
                        if (occur == BooleanClause.Occur.MUST) must++;
                        else if (occur == BooleanClause.Occur.MUST_NOT) mustnot++;
                        else if (occur == BooleanClause.Occur.SHOULD) should++;
                        else if (occur == null) should++;
                    }
                    occur = clause.GetOccur();

                    if (mustnot == 0 && should == 0) // MUST
                    {
                        if (occur == BooleanClause.Occur.MUST)
                            add(newClauses, subClauses, BooleanClause.Occur.MUST);
                        else if (occur == BooleanClause.Occur.MUST_NOT)
                            add(newClauses, subClauses, BooleanClause.Occur.MUST_NOT);
                        else
                            newClauses.Add(clause);
                        changed = true;
                    }
                    else if (must == 0 && should == 0) // MUST_NOT
                    {
                        if (occur == BooleanClause.Occur.MUST)
                            add(newClauses, subClauses, BooleanClause.Occur.MUST_NOT);
                        else if (occur == BooleanClause.Occur.MUST_NOT)
                            add(newClauses, subClauses, BooleanClause.Occur.MUST);
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
                visitedClauses = newClauses.ToArray();

            BooleanQuery newQuery = null;
            if (visitedClauses != clauses)
            {
                newQuery = new BooleanQuery(booleanq.IsCoordDisabled());
                for (int i = 0; i < visitedClauses.Length; i++)
                    newQuery.Add(visitedClauses[i]);
            }
            return newQuery ?? booleanq;
        }

        private void add(List<BooleanClause> newClauses, BooleanClause[] oldClauses, BooleanClause.Occur newOccur)
        {
            foreach (var oldClause in oldClauses)
            {
                oldClause.SetOccur(newOccur);
                newClauses.Add(oldClause);
            }
        }
    }
}
