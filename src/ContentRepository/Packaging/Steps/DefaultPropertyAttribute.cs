using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Packaging.Steps
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultPropertyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    public class AnnotationAttribute : Attribute
    {
        public string Documentation { get; set; }

        public AnnotationAttribute(string documentation)
        {
            this.Documentation = documentation;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class XmlFragmentAttribute : Attribute
    {
    }
}
