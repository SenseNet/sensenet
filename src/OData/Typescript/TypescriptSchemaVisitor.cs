using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Schema.Metadata;
using SenseNet.ContentRepository.Storage.Search;

namespace SenseNet.OData.Typescript
{
    internal abstract class TypescriptSchemaVisitor : SchemaVisitor
    {
        protected TypescriptGenerationContext Context { get; }

        protected TypescriptSchemaVisitor(TypescriptGenerationContext context)
        {
            this.Context = context;
        }
    }
}
