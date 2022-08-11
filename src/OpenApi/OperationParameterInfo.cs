using System;
using System.Diagnostics;

namespace SenseNet.OpenApi
{
    [DebuggerDisplay("{Type} {Name}")]
    public class OperationParameterInfo
    {
        public Type Type { get; set; }
        public string Name { get; set; }
        public bool IsOptional { get; set; }
        public string Documentation { get; set; }
        public string Example { get; set; }
    }
}
