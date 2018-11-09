using System.Linq;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.Search.Querying
{
    internal class SharingFinalizerVisitor : SnQueryVisitor
    {
        public override SnQueryPredicate VisitSimplePredicate(SimplePredicate simplePredicate)
        {
            if (simplePredicate.FieldName != SharingVisitor.Sharing)
                return simplePredicate;

            if (simplePredicate.Value.Type != IndexValueType.StringArray)
                return simplePredicate;

            var value = string.Join(",", simplePredicate.Value.StringArrayValue
                .Distinct()
                .OrderBy(x => "TICML".IndexOf(x[0]))
                .ToArray());

            return new SimplePredicate(simplePredicate.FieldName, new IndexValue(value));
        }
    }
}
