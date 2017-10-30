namespace SenseNet.Search.Querying.Parser.Predicates
{
    public enum Occurence { Default, Should, Must, MustNot }

    public class LogicalClause
    {
        public SnQueryPredicate Predicate { get; }
        public Occurence Occur { get; set; }

        public LogicalClause(SnQueryPredicate predicate, Occurence occur)
        {
            Predicate = predicate;
            Occur = occur;
        }
    }
}
