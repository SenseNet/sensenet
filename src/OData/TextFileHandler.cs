using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using SC = SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Storage;
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

        private static readonly string[] DefaultTextFileExtensions = { /*"md", "txt", "js", "settings"*/ };

        private static string[] TextFileExtensions => SC.Settings.GetValue(
            Settings.SettingsName,
            Settings.Extensions,
            null, DefaultTextFileExtensions);

        private static readonly char[] Whitespaces = "\t\r\n".ToCharArray();

        internal static object ProjectBinaryField(BinaryField field, string[] selection, HttpContext httpContext)
        {
            var allSelected = selection == null || selection.Length == 0 || selection[0] == "*";
            var contentIsFinalized = field?.Content?.ContentHandler.SavingState == ContentSavingState.Finalized;
            var contentType = string.Empty;

            var stream = contentIsFinalized
                ? DocumentBinaryProvider.Instance.GetStream(field.Content.ContentHandler, field.Name, httpContext,
                    out contentType, out var binaryFileName)
                : null;

            var message = string.Empty;
            var contentName = field?.Content?.Name;
            var extension = Path.GetExtension(contentName)?.Trim('.');
            var maxSize = SC.Settings.GetValue(Settings.SettingsName, Settings.MaxExpandableSize, 
                null, 1024 * 1024);

            if (stream?.Length > maxSize)
            {
                message = $"Size limit exceed. Limit: {maxSize}, size: {stream.Length}";
            }
            else if (!contentIsFinalized)
            {
                message = "Content is not finalized.";
            }
            else
            {
                var whitelist = TextFileExtensions;
                if (whitelist.Length > 0 && !whitelist.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    message = $"Not a text file. The *.{extension} is restricted by the file extension list.";
            }

            var text = string.IsNullOrEmpty(message) && contentIsFinalized
                ? ReadBinaryContent(stream, out message)
                : null;

            var result = new Dictionary<string, object>();
            if (allSelected || selection.Contains(Expansion.FileName))
                result.Add(Expansion.FileName, binaryFileName.ToString());
            if (allSelected || selection.Contains(Expansion.ContentType))
                result.Add(Expansion.ContentType, contentType);
            if (allSelected || selection.Contains(Expansion.Length))
                result.Add(Expansion.Length, stream?.Length);
            if (allSelected || selection.Contains(Expansion.Message))
                result.Add(Expansion.Message, message);
            if (allSelected || selection.Contains(Expansion.Text))
                result.Add(Expansion.Text, text);
            return result;
        }
        private static string ReadBinaryContent(Stream stream, out string message)
        {
            var size = Convert.ToInt32(stream.Length);

            var buffer = new char[size];
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                reader.ReadBlockAsync(buffer, 0, size)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

            // cut trailing zero bytes
            while (size > 0 && buffer[size - 1] == (char)0)
                size--;

            // search non-text characters: 0x7f and c < 0x20 except common whitespaces: 0x09, 0x0A, 0x0D (\t \n \r).
            for (var i = 0; i < size; i++)
            {
                var c = buffer[i];
                if (c != (char) 127 && (c >= (char) 32 || Whitespaces.Contains(c)))
                    continue;
                message = "Not a text file. Contains one or more non-text characters";
                return null;
            }

            message = string.Empty;
            return new string(buffer, 0, size);
        }
    }
}
