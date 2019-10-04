using System;
using System.IO;
using System.IO.Compression;
using System.Resources;
using System.Web;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;

namespace Compatibility.SenseNet.Portal.Handlers
{
    /// <summary>
    /// System handler for serving metadata (e.g. generated TypeScript classes) on-the-fly.
    /// </summary>
    [ContentHandler]
    public class GetMetadataApplication : HttpHandlerApplication
    {
        public GetMetadataApplication(Node parent) : this(parent, null) { }
        public GetMetadataApplication(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected GetMetadataApplication(NodeToken nt) : base(nt) { }

        protected virtual string[] ModuleNames { get; } = { "enums", "complextypes", "contenttypes", "resources", "schemas", "fieldsettings" };
        protected virtual string CompressedMetaName { get; } = "meta";

        public new void ProcessRequest(HttpContext context)
        {
            //var response = context.Response;
            //var ext = System.IO.Path.GetExtension(this.Name)?.Trim('.');
            //var module = System.IO.Path.GetFileNameWithoutExtension(this.Name);

            //response.AddHeader("Content-Disposition", "attachment; filename=" + this.Name);
            //response.Clear();

            //switch (ext)
            //{
            //    case "ts":
            //        response.ContentType = "text/x-typescript";

            //        using (var memoryStream = new MemoryStream())
            //        {
            //            using (var sw = new StreamWriter(memoryStream))
            //            {
            //                WriteMetadata(sw, module);

            //                response.AppendHeader("Content-Length", memoryStream.Length.ToString());

            //                memoryStream.Seek(0, SeekOrigin.Begin);
            //                memoryStream.CopyTo(response.OutputStream);
            //            }
            //        }
                    
            //        break;
            //    case "zip":
            //        if (module != CompressedMetaName)
            //            throw new InvalidOperationException("Unknown compressed name: " + this.Name);

            //        response.ContentType = "application/zip";
            //        WriteCompressedMetadata(response.OutputStream);
            //        break;
            //    default:
            //        throw new InvalidOperationException("Unknown extension: " + this.Name);
            //}
            throw new NotImplementedException(); //UNDONE:ODATA Not implemented: GetMetadataApplication.ProcessRequest
        }

        protected virtual void WriteMetadata(TextWriter writer, string requestedModule)
        {
            //var context = new TypescriptGenerationContext();
            //var schema0 = new ContentRepository.Schema.Metadata.Schema(new[]
            //    {"Application", "ApplicationCacheFile", "FieldSettingContent", "JournalNode"});
            //var schema1 = new TypescriptTypeCollectorVisitor(context).Visit(schema0);

            //switch (requestedModule)
            //{
            //    case "enums":
            //        new TypescriptEnumsVisitor(context, writer).Visit(schema1);
            //        break;
            //    case "complextypes":
            //        new TypescriptComplexTypesVisitor(context, writer).Visit(schema1);
            //        break;
            //    case "contenttypes":
            //        new TypescriptClassesVisitor(context, writer).Visit(schema1);
            //        break;
            //    case "resources":
            //        ResourceWriter.WriteResourceClasses(writer);
            //        break;
            //    case "schemas":
            //        new TypescriptCtdVisitor(context, writer).Visit(schema1);
            //        break;
            //    case "fieldsettings":
            //        new TypescriptFieldSettingsVisitor(context, writer).Visit(schema1);
            //        break;
            //    default:
            //        throw new InvalidOperationException(
            //            $"Unknown module name: {requestedModule}. Valid names: {string.Join(", ", ModuleNames)}");
            //}
            throw new NotImplementedException(); //UNDONE:ODATA Not implemented: GetMetadataApplication.WriteMetadata
        }

        protected virtual void WriteCompressedMetadata(Stream outputStream)
        {
            //using (var memoryStream = new MemoryStream())
            //{
            //    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            //    {
            //        foreach (var moduleName in ModuleNames)
            //        {
            //            var moduleEntry = archive.CreateEntry(moduleName + ".ts");

            //            using (var entryStream = moduleEntry.Open())
            //            {
            //                using (var streamWriter = new StreamWriter(entryStream))
            //                {
            //                    WriteMetadata(streamWriter, moduleName);
            //                }
            //            }
            //        }
            //    }

            //    HttpContext.Current.Response.AppendHeader("Content-Length", memoryStream.Length.ToString());

            //    memoryStream.Seek(0, SeekOrigin.Begin);
            //    memoryStream.CopyTo(outputStream);
            //}
            throw new NotImplementedException(); //UNDONE:ODATA Not implemented: GetMetadataApplication.WriteCompressedMetadata
        }
    }
}
