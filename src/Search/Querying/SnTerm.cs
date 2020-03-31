using System;
using System.Diagnostics;

namespace SenseNet.Search.Querying
{
    /// <summary>
    /// Represents a name-value pair in the querying and indexing.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{Name}:{ValueAsString}:{Type}")]
    public class SnTerm : IndexValue
    {
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.String value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.String value</param>
        public SnTerm(string name, string value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named array of System.String value.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">Array of System.String value</param>
        public SnTerm(string name, string[] value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Boolean value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Boolean value</param>
        public SnTerm(string name, bool value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Int32 value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Int32 value</param>
        public SnTerm(string name, int value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Int64 value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Int64 value</param>
        public SnTerm(string name, long value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Single value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Single value</param>
        public SnTerm(string name, float value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.Double value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.Double value</param>
        public SnTerm(string name, double value) : base(value) { Name = name; }
        /// <summary>
        /// Initializes an instance of the SnTerm with a named System.DateTime value
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="value">System.DateTime value</param>
        public SnTerm(string name, DateTime value) : base(value) { Name = name; }

        /// <summary>
        /// Gets the name of the term.
        /// </summary>
        public string Name { get; }

        public override string ToString()
        {
            return $"{Name}:{base.ToString()}";
        }
    }
}
