using System;
using System.Diagnostics;

namespace SenseNet.Search.Querying
{
    [Serializable]
    [DebuggerDisplay("{Name}:{ValueAsString}:{Type}")]
    public class SnTerm : IndexValue
    {
        public SnTerm(string name, string value) : base(value) { Name = name; }
        public SnTerm(string name, string[] value) : base(value) { Name = name; }
        public SnTerm(string name, bool value) : base(value) { Name = name; }
        public SnTerm(string name, int value) : base(value) { Name = name; }
        public SnTerm(string name, long value) : base(value) { Name = name; }
        public SnTerm(string name, float value) : base(value) { Name = name; }
        public SnTerm(string name, double value) : base(value) { Name = name; }
        public SnTerm(string name, DateTime value) : base(value) { Name = name; }

        public string Name { get; }
    }
}
