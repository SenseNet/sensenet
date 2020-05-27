using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using SC = SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.Portal.Virtualization;

namespace SenseNet.OData
{

    internal class TextFileHandler
    {
        private static class Expansion
        {
            public static string FileName = "FileName";
            public static string ContentType = "ContentType";
            public static string Length = "Length";
            public static string Message = "Message";
            public static string Text = "Text";
        }
        private static class Settings
        {
            public static string SettingsName = "TextFiles";
            public static string Extensions = "Extensions";
            public static string MaxExpandableSize = "MaxExpandableSize";
        }

        private static readonly string[] DefaultTextFileExtensions = { "md", "txt", "js", "settings" };

        private static string[] TextFileExtensions => SC.Settings.GetValue(
            Settings.SettingsName,
            Settings.Extensions,
            null, DefaultTextFileExtensions);

        internal static object ProjectBinaryField(BinaryField field, string[] selection, HttpContext httpContext)
        {
            var allSelected = selection == null || selection.Length == 0 || selection[0] == "*";

            var stream = DocumentBinaryProvider.Instance.GetStream(field.Content.ContentHandler,
                field.Name, httpContext, out var contentType, out var binaryFileName);

            string message;
            var contentName = field.Content.Name;
            var extension = Path.GetExtension(contentName)?.Trim('.');
            var maxSize = SC.Settings.GetValue(Settings.SettingsName, Settings.MaxExpandableSize, 
                null, 500 * 1024);

            if (stream.Length > maxSize)
                message = $"Size limit exceed. Limit: {maxSize}, size: {stream.Length}";
            else if (!TextFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                message = $"*.{extension} is not a text file.";
            else
                message = string.Empty;

            var result = new Dictionary<string, object>();
            if (allSelected || selection.Contains(Expansion.FileName))
                result.Add(Expansion.FileName, binaryFileName.ToString());
            if (allSelected || selection.Contains(Expansion.ContentType))
                result.Add(Expansion.ContentType, contentType);
            if (allSelected || selection.Contains(Expansion.Length))
                result.Add(Expansion.Length, stream.Length);
            if (allSelected || selection.Contains(Expansion.Message))
                result.Add(Expansion.Message, message);
            if (allSelected || selection.Contains(Expansion.Text))
                result.Add(Expansion.Text, string.IsNullOrEmpty(message) ? ReadBinaryContent(stream) : null);
            return result;
        }
        private static string ReadBinaryContent(Stream stream)
        {
            var size = Convert.ToInt32(stream.Length);

            var buffer = new char[size];
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                reader.ReadBlockAsync(buffer, 0, size)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

            // cut trailing zero bytes
            while (size > 0 && buffer[size - 1] == (char)0)
                size--;

            return new string(buffer, 0, size);
        }
    }
}
