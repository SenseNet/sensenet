using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Schema.Metadata;

namespace SenseNet.OData.Typescript
{
    internal class TypescriptTypeCollectorVisitor : TypescriptSchemaVisitor
    {
        public TypescriptTypeCollectorVisitor(TypescriptGenerationContext context) : base(context) { }

        protected override IMetaNode VisitComplexType(ComplexType complexType)
        {
            var complexTypes = Context.ComplexTypes;
            if (complexTypes.All(c => c.Name != complexType.Name))
                complexTypes.Add(complexType);
            return complexType;
        }
        protected override IMetaNode VisitEnumeration(Enumeration enumeration)
        {
            var visitedEnumeration = base.VisitEnumeration(enumeration);

            var enumKey = enumeration.Key;
            var enumName = enumeration.FieldSetting.Name;
            var fullName = Context.GetEnumerationFullName(enumeration);

            if (Context.Enumerations.All(e => e.Key != enumKey))
                Context.Enumerations.Add(enumeration);

            string existingKey;
            if (Context.EmittedEnumerationNames.TryGetValue(enumName, out existingKey))
            {
                if (existingKey != enumeration.Key)
                    Context.EmittedEnumerationNames.Add(fullName, enumKey);
            }
            else
            {
                Context.EmittedEnumerationNames.Add(enumName, enumKey);
            }

            return visitedEnumeration;
        }
    }
}
