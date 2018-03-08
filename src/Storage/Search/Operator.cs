// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Search.Querying
{
    /// <summary>
    /// Defines query operators used in programmatic queries in the NodeQuery class.
    /// </summary>
    public enum Operator
    {
        /// <summary>
        /// Represents the "equality" operator.
        /// </summary>
        Equal,
        /// <summary>
        /// Represents the "not equality" operator.
        /// </summary>
        NotEqual,
        /// <summary>
        /// Represents the "less than" operator.
        /// </summary>
        LessThan,
        /// <summary>
        /// Represents the "greater than" operator.
        /// </summary>
        GreaterThan,
        /// <summary>
        /// Represents the "less than or equal" operator.
        /// </summary>
        LessThanOrEqual,
        /// <summary>
        /// Represents the "greater than or equal" operator.
        /// </summary>
        GreaterThanOrEqual,
        /// <summary>
        /// Represents the "starts with" operator.
        /// </summary>
        StartsWith,
        /// <summary>
        /// Represents the "ends with" operator.
        /// </summary>
        EndsWith,
        /// <summary>
        /// Represents the "contains" operator.
        /// </summary>
        Contains
    }
}
