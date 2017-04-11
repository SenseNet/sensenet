using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Schema.Metadata;

namespace SenseNet.Portal.OData.Typescript
{
    internal class TypescriptCtdVisitor : TypescriptModuleWriter
    {
        public TypescriptCtdVisitor(TypescriptGenerationContext context, TextWriter writer) : base(context, writer) { }

        protected override IMetaNode VisitSchema(ContentRepository.Schema.Metadata.Schema schema)
        {
            #region Write filestart
            _writer.WriteLine(@"import { " + TypescriptGenerationContext.FieldSettingsModuleName + @" } from './" + TypescriptGenerationContext.FieldSettingsModuleName + @"';
/**
 * Module for ContentType schemas.
 *
 * A Content Type Definition in Sense/Net is an xml-format configuration file for defining Content Types. The xml configuration (CTD) holds information about the types name and description
 * properties that control how content of this type look and behave (icon, preview generation, indexing), set of fields, etc. This information about the type and its Fields helps us for example
 * building forms. Based on the Field definitions we can render a Field with its DisplayName as a label or validate the Field on save by its validation related configs.
 *
 * This module provides us description of this Content schemas in Typesript.
 *
 * The ```Schema``` class represents an object that holds the basic information about the Content Type (name, icon, ect.) and an array of its ```FieldSettings``` and their full configuration.
 */

export module " + TypescriptGenerationContext.SchemaModuleName + @" {
    /**
     * Class that represents a Schema.
     *
     * It represents an object that holds the basic information about the Content Type (name, icon, ect.) and an array of its ```FieldSettings``` and their full configuration.
     */
    export class Schema {
        Icon: string;
        DisplayName: string;
        Description: string;
        AllowIndexing: boolean;
        AllowIncrementalNaming: boolean;
        AllowedChildTypes: string[];
        FieldSettings: FieldSettings.FieldSetting[];
        /**
         * @constructs Schema
         * @param options {Object} An object implementing ISchemaOptions interface;
         */
        constructor(options: ISchemaOptions) {
            this.Icon = options.Icon;
            this.DisplayName = options.DisplayName;
            this.Description = options.Description;
            this.FieldSettings = options.FieldSettings;
            this.AllowIndexing = options.AllowIndexing;
            this.AllowIncrementalNaming = options.AllowIncrementalNaming;
            this.AllowedChildTypes = options.AllowedChildTypes;
        }
    }

    /**
    * Interface for classes that represent a Schema.
    *
    * @interface ISchemaOptions
    */
    export interface ISchemaOptions {
        Icon?: string;
        DisplayName?: string;
        Description?: string;
        AllowIndexing?: boolean;
        AllowIncrementalNaming?: boolean;
        AllowedChildTypes?: string[];
        FieldSettings?: FieldSettings.FieldSetting[];
    }
");
            #endregion

            // Do not call base because only classes will be read.
            _indentCount++;
            Visit(schema.Classes);
            _indentCount--;

            #region Write fileend
            _writer.WriteLine(@"}
");
            #endregion
            return schema;
        }

        protected override IMetaNode VisitClass(Class @class)
        {
            var contentType = @class.ContentType;
            var allowedChildTypes = string.Join("', '", contentType.AllowedChildTypeNames);
            if (allowedChildTypes.Length > 0)
                allowedChildTypes = "'" + allowedChildTypes + "'";

            WriteLine("/**");
            WriteLine($" * Method that returns the Content Type Definition of the {contentType.Name}");
            WriteLine(" * @returns {Schema}");
            WriteLine(" */");
            WriteLine($"export function {contentType.Name}CTD(): Schema {{");
            _indentCount++;

            WriteLine($"let options: ISchemaOptions = {{");
            WriteLine($"    DisplayName: '{contentType.DisplayName}',");
            WriteLine($"    Description: '{contentType.Description}',");
            WriteLine($"    Icon: '{contentType.Icon}',");
            WriteLine($"    AllowIndexing: {contentType.IndexingEnabled.ToString().ToLowerInvariant()},");
            WriteLine($"    AllowIncrementalNaming: {contentType.AllowIncrementalNaming.ToString().ToLowerInvariant()},");
            WriteLine($"    AllowedChildTypes: [{allowedChildTypes}],");
            WriteLine($"    FieldSettings: []");
            WriteLine($"}};");
            WriteLine();
            WriteLine($"let schema = new Schema(options);");
            WriteLine();

            var visitedClass = base.VisitClass(@class);

            WriteLine($"return schema;");
            _indentCount--;
            WriteLine("}");

            return visitedClass;
        }
        protected override IEnumerable<Property> VisitProperties(IEnumerable<Property> properties)
        {
            if (properties.Any(p => Context.PropertyBlacklist.Contains(p.Name)))
                return base.VisitProperties(properties.Where(p => p.IsLocal && !Context.PropertyBlacklist.Contains(p.Name)).ToArray());
            return base.VisitProperties(properties);
        }
        protected override IMetaNode VisitProperty(Property property)
        {
            var propertyLines = new List<string>();
            var propertyInfos = property.FieldSetting.GetType().GetProperties()
                .Where(p => !FieldSettingPropertyBlackList.Contains(p.Name));
            foreach (var propertyInfo in propertyInfos)
            {
                var name = propertyInfo.Name;
                var value = GetPropertyValue(property.FieldSetting, propertyInfo);
                if (value != null)
                    propertyLines.Add(name.ToCamelCase() + ": " + value);
            }


            WriteLine("schema.FieldSettings.push(");
            _indentCount++;
            WriteLine($"new FieldSettings.{property.FieldSetting.GetType().Name}({{");
            _indentCount++;
            for (int i = 0; i < propertyLines.Count; i++)
            {
                var comma = i < propertyLines.Count - 1 ? "," : "";
                WriteLine($"{propertyLines[i]}{comma}");
            }
            _indentCount--;
            WriteLine("}));");
            _indentCount--;
            return base.VisitProperty(property);
        }
    }
}
