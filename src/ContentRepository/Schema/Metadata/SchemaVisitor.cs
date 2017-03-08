using System;
using System.Activities.Statements;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.ContentRepository.Schema.Metadata
{
    public abstract class SchemaVisitor
    {
        public virtual IMetaNode Visit(IMetaNode item)
        {
            var schema = item as Schema; if (schema != null) return VisitSchema(schema);
            var @class = item as Class; if (@class != null) return VisitClass(@class);
            var property = item as Property; if (property != null) return VisitProperty(property);
            var simpleType = item as SimpleType; if (simpleType != null) return VisitSimpleType(simpleType);
            var referenceType = item as ReferenceType; if (referenceType != null) return VisitReferenceType(referenceType);
            var complexType = item as ComplexType; if (complexType != null) return VisitComplexType(complexType);
            var enumeration = item as Enumeration; if (enumeration != null) return VisitEnumeration(enumeration);
            var option = item as EnumOption; if (option != null) return VisitEnumOption(option);

            throw new NotSupportedException($"{item.GetType().FullName} is not supported here.");
        }
        public virtual IEnumerable<IMetaNode> Visit(IEnumerable<IMetaNode> item)
        {
            var enumerations = item as IEnumerable<Enumeration>; if (enumerations != null) return VisitEnumerations(enumerations);
            var complexTypes = item as IEnumerable<ComplexType>; if (complexTypes != null) return VisitComplexTypes(complexTypes);

            var classes = item as IEnumerable<Class>; if (classes != null) return VisitClasses(classes);
            var properties = item as IEnumerable<Property>; if (properties != null) return VisitProperties(properties);
            var options = item as IEnumerable<EnumOption>; if (options != null) return VisitEnumOptions(options);

            throw new NotSupportedException($"{item.GetType().FullName} is not supported here.");
        }

        protected virtual IMetaNode VisitSchema(Schema schema)
        {
            var visitedClasses = (IEnumerable<Class>)Visit(schema.Classes);
            if (visitedClasses == schema.Classes)
                return schema;
            return schema.Rewrite(visitedClasses);
        }
        private IEnumerable<IMetaNode> VisitEnumerations(IEnumerable<Enumeration> enumerations)
        {
            var origEnums = enumerations.ToArray();
            var newCEnums = new Enumeration[origEnums.Length];
            var rewrite = false;
            for (int i = 0; i < origEnums.Length; i++)
            {
                newCEnums[i] = (Enumeration)Visit(origEnums[i]);
                rewrite |= newCEnums[i] != origEnums[i];
            }
            return rewrite ? newCEnums : enumerations;
        }
        private IEnumerable<IMetaNode> VisitComplexTypes(IEnumerable<ComplexType> complexTypes)
        {
            var origComplexTypes = complexTypes.ToArray();
            var newComplexTypes = new ComplexType[origComplexTypes.Length];
            var rewrite = false;
            for (int i = 0; i < origComplexTypes.Length; i++)
            {
                newComplexTypes[i] = (ComplexType)Visit(origComplexTypes[i]);
                rewrite |= newComplexTypes[i] != origComplexTypes[i];
            }
            return rewrite ? newComplexTypes : complexTypes;
        }
        protected virtual IEnumerable<Class> VisitClasses(IEnumerable<Class> classes)
        {
            var origClasses = classes.ToArray();
            var newClasses = new Class[origClasses.Length];
            var rewrite = false;
            for (int i = 0; i < origClasses.Length; i++)
            {
                newClasses[i] = (Class)Visit(origClasses[i]);
                rewrite |= newClasses[i] != origClasses[i];
            }
            return rewrite ? newClasses : classes;
        }
        protected virtual IMetaNode VisitClass(Class @class)
        {
            var visitedProperties = (IEnumerable<Property>)Visit(@class.Properties);
            if (visitedProperties == @class.Properties)
                return @class;
            return @class.Rewrite(visitedProperties);
        }
        protected virtual IEnumerable<Property> VisitProperties(IEnumerable<Property> properties)
        {
            var origProperties = properties.ToArray();
            var newProperties = new Property[origProperties.Length];
            var rewrite = false;
            for (int i = 0; i < origProperties.Length; i++)
            {
                newProperties[i] = (Property)Visit(origProperties[i]);
                rewrite |= newProperties[i] != origProperties[i];
            }
            return rewrite ? newProperties : properties;
        }
        protected virtual IMetaNode VisitProperty(Property property)
        {
            var visitedPropertyType = (PropertyType)Visit(property.Type);
            if (visitedPropertyType == property.Type)
                return property;
            return property.Rewrite(visitedPropertyType);
        }
        protected virtual IMetaNode VisitSimpleType(SimpleType simpleType)
        {
            // do nothing
            return simpleType;
        }
        protected virtual IMetaNode VisitReferenceType(ReferenceType referenceType)
        {
            // do nothing
            return referenceType;
        }
        protected virtual IMetaNode VisitComplexType(ComplexType complexType)
        {
            // do nothing
            return complexType;
        }
        protected virtual IMetaNode VisitEnumeration(Enumeration enumeration)
        {
            var visitedOptions = (IEnumerable<EnumOption>)Visit(enumeration.Options);
            if (enumeration.Options == visitedOptions)
                return enumeration;
            return enumeration.Rewrite(visitedOptions.ToArray());
        }
        protected virtual IEnumerable<EnumOption> VisitEnumOptions(IEnumerable<EnumOption> options)
        {
            var origOptions = options.ToArray();
            var newOptions = new EnumOption[origOptions.Length];
            var rewrite = false;
            for (int i = 0; i < origOptions.Length; i++)
            {
                newOptions[i] = (EnumOption)Visit(origOptions[i]);
                rewrite |= newOptions[i] != origOptions[i];
            }
            return rewrite ? newOptions: options;
        }
        protected virtual IMetaNode VisitEnumOption(EnumOption option)
        {
            // do nothing
            return option;
        }
    }
}
