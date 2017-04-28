﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Schema.Metadata;

namespace SenseNet.Portal.OData.Typescript
{
    internal class TypescriptEnumsVisitor : TypescriptModuleWriter
    {
        public TypescriptEnumsVisitor(TypescriptGenerationContext context, TextWriter writer) : base(context, writer) { }

        protected override IMetaNode VisitSchema(ContentRepository.Schema.Metadata.Schema schema)
        {
            // do not call the base functionality

            #region Write filestart
            _writer.WriteLine(@"/**
 * Module for enums types defined in SenseNet helps you to use enums with dot notation. 
 *
 * This module is autogenerated from Sense/Net metadata (/Odata/$metadata)
 *
 * ```
 * let car = new ContentTypes.Car({
 *  Id: 1,
 *  Name: 'MyCar',
 *  DisplayName: 'My Car',
 *  Style: Enum.Style.Cabrio
 * });
 * ```
 */

export module " + TypescriptGenerationContext.EnumTypesModuleName + @" {");
            #endregion

            _indentCount++;
            foreach (var enumeration in Context.Enumerations)
                Visit(enumeration);
            _indentCount--;

            #region Write fileend
            _writer.WriteLine(@"}
");
            #endregion

            return schema;
        }

        protected override IMetaNode VisitEnumeration(Enumeration enumeration)
        {
            // do not call base functionality in this method

            var options = string.Join(", ", enumeration.Options.Select(o => o.Name).ToArray());

            var names = Context.EmittedEnumerationNames
                .Where(x => x.Value == enumeration.Key)
                .Select(x => x.Key)
                .ToArray();
            foreach (var name in names)
                WriteLine($"export enum {name} {{ {options} }}");

            return enumeration;
        }
    }
}
