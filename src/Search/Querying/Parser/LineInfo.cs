using System.Diagnostics;

namespace SenseNet.Search.Querying.Parser
{
	[DebuggerDisplay("[Line: {Line}, Col: {Column}]")]
	public class LineInfo
	{
		internal static LineInfo NullValue = new LineInfo(0, 0);
		public int Line { get; }
		public int Column { get; }

		internal LineInfo(int line, int column)
		{
			Line = line;
			Column = column;
		}

        public override string ToString()
        {
            return $"[Line: {Line + 1}, Col: {Column + 1}]";
        }
	}
}
