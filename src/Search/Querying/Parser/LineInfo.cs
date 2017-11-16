using System.Diagnostics;

namespace SenseNet.Search.Querying.Parser
{
    /// <summary>
    /// Represents a position in a CQL query text.
    /// Instantiated in the CQL parsing process.
    /// </summary>
	[DebuggerDisplay("[Line: {Line}, Col: {Column}]")]
	public class LineInfo
	{
		internal static LineInfo NullValue = new LineInfo(0, 0);
        /// <summary>
        /// Number of current line (first: 1).
        /// </summary>
		public int Line { get; }
	    /// <summary>
	    /// Number of current character in the current line (first: 1).
	    /// </summary>
		public int Column { get; }

		internal LineInfo(int line, int column)
		{
			Line = line;
			Column = column;
		}

        /// <summary>
        /// String representation of this LineInfo instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[Line: {Line + 1}, Col: {Column + 1}]";
        }
	}
}
