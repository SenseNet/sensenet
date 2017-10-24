using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SenseNet.ContentRepository.Schema.Metadata;

namespace SenseNet.Portal.OData.Typescript
{
    internal class TypescriptClassesVisitor : TypescriptModuleWriter
    {
        public TypescriptClassesVisitor(TypescriptGenerationContext context, TextWriter writer) : base(context, writer) { }

        protected override IMetaNode VisitSchema(ContentRepository.Schema.Metadata.Schema schema)
        {
            _writer.WriteLine(@"/**
 *
 * @module ContentTypes
 * @preferred
 *
 *
 * @description The Content Repository contains many different types of ```Content```. ```Content``` vary in structure and even in function. Different types of content contain different fields,
 * are displayed with different views, and may also implement different business logic. The fields, views and business logic of a content is defined by its type - the Content Type.
 *
 * Content Types are defined in a type hierarchy: a Content Type may be inherited from another Content Type - thus automatically inheriting its fields.
 *
 * This module represents the above mentioned type hierarchy by Typescript classes with the Content Types' Fields as properties. With Typescript classes we can derive types from another
 * inheriting its properties just like Content Types in the Content Repository. This module provides us to create an objects with a type so that we can validate on its properties by their
 * types or check the required ones.
 *
 *//** */
import { Enums, ComplexTypes } from './SN';
import { ContentListReferenceField, ContentReferenceField } from './ContentReferences';


");

            // Do not call base because only classes will be read.
            Visit(schema.Classes);

            return schema;
        }
        protected override IMetaNode VisitClass(Class @class)
        {
            var visitedProperties = ((IEnumerable<Property>)Visit(@class.Properties)).ToArray();
            var propertyLines = new List<string>();
            foreach (var property in visitedProperties)
            {
                propertyLines.Add($"{property.Name}?: {GetPropertyTypeName(property)};");
            }

            var type = @class.Name;
            var parentName = @class.BaseClassName;
            WriteLine($"/**");
            WriteLine($" * Class representing a {type}");
            WriteLine($" * @class {type}");
            if (!string.IsNullOrWhiteSpace(parentName))
            {
                WriteLine($" * @extends {{@link {parentName}" + "}");
            }
            WriteLine($" */");
            if (!string.IsNullOrWhiteSpace(parentName))
            {
                WriteLine($"export class {type} extends {parentName} {{");
            }
            else
            {
                WriteLine($"export class {type} {{");
            }
            
            _indentCount++;
            foreach (var propertyLine in propertyLines)
                WriteLine(propertyLine);
            WriteLine();
            _indentCount--;
            WriteLine("}");

            WriteLine();

            if (@class.Properties == visitedProperties)
                return @class;
            return @class.Rewrite(visitedProperties);
        }

        protected override IEnumerable<Property> VisitProperties(IEnumerable<Property> properties)
        {
            return base.VisitProperties(properties)
                .Where(p => p.IsLocal && !Context.PropertyBlacklist.Contains(p.Name))
                .ToArray();
        }
    }
}
