using System.Collections.Generic;
using System.IO;
using System.Linq;
using SenseNet.ContentRepository.Schema.Metadata;

namespace SenseNet.OData.Typescript
{
    internal class TypescriptCtdVisitor : TypescriptModuleWriter
    {
        public TypescriptCtdVisitor(TypescriptGenerationContext context, TextWriter writer) : base(context, writer) { }

        protected override IMetaNode VisitSchema(ContentRepository.Schema.Metadata.Schema schema)
        {
            #region Write filestart
            _writer.WriteLine(@"/**
 * @module Schemas
 * @preferred
 * @description Module for ContentType schemas.
 *
 * A Content Type Definition in Sense/Net is an xml-format configuration file for defining Content Types. The xml configuration (CTD) holds information about the types name and description
 * properties that control how content of this type look and behave (icon, preview generation, indexing), set of fields, etc. This information about the type and its Fields helps us for example
 * building forms. Based on the Field definitions we can render a Field with its DisplayName as a label or validate the Field on save by its validation related configs.
 *
 * This module provides us description of this Content schemas in Typesript.
 *
 * The ```Schema``` class represents an object that holds the basic information about the Content Type (name, icon, ect.) and an array of its ```FieldSettings``` and their full configuration.
 */

import * as FieldSettings from './FieldSettings'

/**
 * Class that represents a Schema.
 *
 * It represents an object that holds the basic information about the Content Type (name, icon, ect.) and an array of its ```FieldSettings``` and their full configuration.
 */
export class Schema {
     public ContentTypeName!: string
     public ParentTypeName?: string
     public Icon!: string
     public DisplayName!: string
     public Description!: string
     public AllowIndexing!: boolean
     public AllowIncrementalNaming!: boolean
     public AllowedChildTypes!: string[]
     public FieldSettings!: FieldSettings.FieldSetting[]
}

export const SchemaStore: Schema[] = [
");
            #endregion

            // Do not call base because only classes will be read.
            _indentCount++;
            Visit(schema.Classes);
            _indentCount--;

            _writer.WriteLine(@"];");
            _writer.WriteLine();
            return schema;
        }

        protected override IMetaNode VisitClass(Class @class)
        {
            var contentType = @class.ContentType;
            var allowedChildTypes = string.Join("\", \"", contentType.AllowedChildTypeNames);
            if (allowedChildTypes.Length > 0)
                allowedChildTypes = "\"" + allowedChildTypes + "\"";
            WriteLine("{");
            _indentCount++;
            WriteLine($"ContentTypeName: \"{contentType.Name}\",");
            if (!string.IsNullOrWhiteSpace(contentType.ParentTypeName))
            {
                WriteLine($"ParentTypeName: \"{contentType.ParentTypeName}\",");
            }
            WriteLine($"DisplayName: \"{contentType.DisplayName}\",");
            WriteLine($"Description: \"{contentType.Description}\",");
            WriteLine($"Icon: \"{contentType.Icon}\",");
            WriteLine($"AllowIndexing: {contentType.IndexingEnabled.ToString().ToLowerInvariant()},");
            WriteLine($"AllowIncrementalNaming: {contentType.AllowIncrementalNaming.ToString().ToLowerInvariant()},");
            WriteLine($"AllowedChildTypes: [{allowedChildTypes}],");
            WriteLine($"HandlerName: \"{contentType.HandlerName}\",");
            WriteLine($"FieldSettings: [");

            var visitedClass = base.VisitClass(@class);

            WriteLine($"]");
            WriteLine($"}},");

            _indentCount--;

            return visitedClass;
        }
        protected override IEnumerable<Property> VisitProperties(IEnumerable<Property> properties)
        {
            if (properties.Any(p => TypescriptGenerationContext.PropertyBlacklist.Contains(p.Name)))
                return base.VisitProperties(properties.Where(p => p.IsLocal && !TypescriptGenerationContext.PropertyBlacklist.Contains(p.Name)).ToArray());
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
                if (name == "Type")
                    continue;
                var value = GetPropertyValue(property.FieldSetting, propertyInfo);
                if (value != null)
                    propertyLines.Add($"{name}: {value}");
            }
            _indentCount++;
            WriteLine("{");
            _indentCount++;
            WriteLine($"Type: \"{property.FieldSetting.GetType().Name}\",");
            for (int i = 0; i < propertyLines.Count; i++)
            {
                var comma = i < propertyLines.Count - 1 ? "," : "";
                WriteLine($"{propertyLines[i]}{comma}");
            }
            _indentCount--;
            WriteLine($"}} as FieldSettings.{property.FieldSetting.GetType().Name},");
            _indentCount--;
            return base.VisitProperty(property);
        }
    }
}
