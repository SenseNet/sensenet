using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Schema.Metadata;

namespace SenseNet.Portal.OData.Typescript
{
    internal class TypescriptClassesVisitor : TypescriptModuleWriter
    {
        public TypescriptClassesVisitor(TypescriptGenerationContext context, TextWriter writer) : base(context, writer) { }

        protected override IMetaNode VisitSchema(ContentRepository.Schema.Metadata.Schema schema)
        {
            _writer.WriteLine(@"//file: ContentTypes.ts
import { " + TypescriptGenerationContext.EnumTypesModuleName + @" } from './" + TypescriptGenerationContext.EnumTypesModuleName + @"';
import { FieldSettings } from './FieldSettings';
import { " + TypescriptGenerationContext.ComplexTypesModuleName + @" } from './" + TypescriptGenerationContext.ComplexTypesModuleName + @"';
import { Content, IContentOptions } from './Content';

/**
 * The Content Repository contains many different types of ```Content```. ```Content``` vary in structure and even in function. Different types of content contain different fields,
 * are displayed with different views, and may also implement different business logic. The fields, views and business logic of a content is defined by its type - the Content Type.
 *
 * Content Types are defined in a type hierarchy: a Content Type may be inherited from another Content Type - thus automatically inheriting its fields.
 *
 * This module represents the above mentioned type hierarchy by Typescript classes with the Content Types' Fields as properties. With Typescript classes we can derive types from another
 * inheriting its properties just like Content Types in the Content Repository. This module provides us to create an objects with a type so that we can validate on its properties by their
 * types or check the required ones.
 */

export module ContentTypes {");

            // Do not call base because only classes will be read.
            _indentCount++;
            Visit(schema.Classes);
            _indentCount--;

            _writer.WriteLine(@"}

/**
 * Creates a Content object by the given type and options Object that hold the field values.
 * @param type {string} The Content will be a copy of the given type.
 * @param options {SenseNet.IContentOptions} Optional list of fields and values.
 * @returns {SenseNet.Content}
 * ```ts
 * var content = SenseNet.Content.Create('Folder', { DisplayName: 'My folder' }); // content is an instance of the Folder with the DisplayName 'My folder'
 * ```
 */
export function CreateContent<T>(type: string, options: IContentOptions = {}): Content {
    let content = new ContentTypes[type](options);
    return content;
}");
            return schema;
        }
        protected override IMetaNode VisitClass(Class @class)
        {
            var visitedProperties = ((IEnumerable<Property>)Visit(@class.Properties)).ToArray();
            var propertyLines = new List<string>();
            foreach (var property in visitedProperties)
            {
                var required = Context.RequiredProperties.Contains(property.Name) ? "" : (property.Type.Required ? "" : "?");
                propertyLines.Add($"{property.Name}{required}: {GetPropertyTypeName(property)};");
            }

            var type = @class.Name;
            var parentName = @class.BaseClassName ?? "Content";
            WriteLine($"/**");
            WriteLine($" * Class representing a {type}");
            WriteLine($" * @class {type}");
            WriteLine($" * @extends {{@link {parentName}" + "}");
            WriteLine($" */");
            WriteLine($"export class {type} extends {parentName} {{");
            _indentCount++;
            foreach (var propertyLine in propertyLines)
                WriteLine(propertyLine);
            WriteLine();

            WriteLine($"/**");
            WriteLine($" * @constructs {type}");
            WriteLine($" * @param options {{object}} An object implementing {{@link I{type}Options" + "} interface");
            WriteLine($" */");
            WriteLine($"constructor(options: I{type}Options) {{");
            WriteLine($"    super(options);");

            _indentCount++;
            foreach (var property in visitedProperties)
                WriteLine($"this.{property.Name} = options.{property.Name};");
            _indentCount--;
            WriteLine("}");
            WriteLine();
            _indentCount--;
            WriteLine("}");

            WriteLine($"/**");
            WriteLine($" * Interface for classes that represent a {type}.");
            WriteLine($" * @interface I{type}Options");
            WriteLine($" * @extends {{@link I{parentName}Options" + "}");
            WriteLine($" */");
            WriteLine($"interface I{type}Options extends I{parentName}Options {{");
            _indentCount++;
            foreach (var propertyLine in propertyLines)
                WriteLine(propertyLine);
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
