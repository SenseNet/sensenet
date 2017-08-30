using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SenseNet.Search.Parser
{
	[DebuggerDisplay("[Line: {Line}, Col: {Column}]")]
	public class LineInfo_OLD //UNDONE:!!! Delete ASAP
    {
        internal static LineInfo_OLD NullValue = new LineInfo_OLD(0, 0);
		public int Line { get; private set; }
		public int Column { get; private set; }

		internal LineInfo_OLD(int line, int column)
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
