using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SenseNet.Portal.OData.Metadata
{
    public class FunctionImport : NamedItem
    {
        public string ReturnType;
        public EntitySet EntitySet;
        public bool IsSideEffecting;
        public bool IsBindable;
        public bool IsComposable;
        public List<Parameter> Parameters;

        public override void WriteXml(TextWriter writer)
        {
            writer.Write("        <FunctionImport ");
            WriteAttribute(writer, "Name", Name);
            WriteAttribute(writer, "ReturnType", ReturnType);
            WriteAttribute(writer, "IsSideEffecting", IsSideEffecting.ToString().ToLower());
            WriteAttribute(writer, "IsBindable", IsBindable.ToString().ToLower());
            WriteAttribute(writer, "IsComposable", IsComposable.ToString().ToLower());
            if (EntitySet != null)
                WriteAttribute(writer, "EntitySet", EntitySet.Name);
            writer.WriteLine(">");

            WriteCollectionXml(writer, Parameters);

            writer.WriteLine("        </FunctionImport>");
        }
    }
}
