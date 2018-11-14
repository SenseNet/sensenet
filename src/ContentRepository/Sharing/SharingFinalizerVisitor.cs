using System.Linq;
using SenseNet.Search;
using SenseNet.Search.Querying;
using SenseNet.Search.Querying.Parser.Predicates;

namespace SenseNet.ContentRepository.Sharing
{
    /// <summary>
    /// Rearranges sharing term values to match the order stored in the index
    /// and combines them into a single index value. It does not modify other
    /// types of values.
    /// </summary>
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
