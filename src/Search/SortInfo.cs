using System;
using System.Diagnostics;

namespace SenseNet.Search
{
    /// <summary>
    /// Represents a sorting criteria for querying.
    /// </summary>
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class SortInfo
    {
        /// <summary>
        /// Gets the field name.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Gets the sorting direction. "False" means ascending, "true" means descending.
        /// </summary>
        public bool Reverse { get; }

        /// <summary>
        /// Initializes an instance of the SortInfo
        /// </summary>
        /// <param name="fieldName">Name of the field. Cannot be null or empty.</param>
        /// <param name="reverse">Direction of the sorting. "False" means ascending, "true" means descending. "False" is the default.</param>
        /// <exception cref="ArgumentNullException">Thrown when fieldName parameter is null.</exception>  
        /// <exception cref="ArgumentException">Thrown when fieldName parameter is empty.</exception>  
        public SortInfo(string fieldName, bool reverse = false)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName));
            if (fieldName.Length == 0)
                throw new ArgumentException($"{nameof(fieldName)} cannot be empty.");
            FieldName = fieldName;
            Reverse = reverse;
        }

        /// <summary>Retrieves a string representation of this instance in the following format: {FieldName} ASC|DESC.</summary>
        public override string ToString()
        {
            return $"{FieldName} {(Reverse ? "DESC" : "ASC")}";
        }
    }
}
