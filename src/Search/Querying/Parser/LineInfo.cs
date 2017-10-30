using System;
using System.Diagnostics;

namespace SenseNet.Search.Querying.Parser
{
	[DebuggerDisplay("[Line: {Line}, Col: {Column}]")]
	public class LineInfo
	{
		internal static LineInfo NullValue = new LineInfo(0, 0);
		public int Line { get; private set; }
		public int Column { get; private set; }

		internal LineInfo(int line, int column)
		{
			Line = line;
			Column = column;
		}

        public override string ToString()
        {
            return String.Format("[Line: {0}, Col: {1}]", Line + 1, Column + 1);
        }
	}
}
