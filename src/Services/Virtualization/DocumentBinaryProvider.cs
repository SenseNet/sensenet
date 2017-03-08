using System;
using System.IO;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Security.ADSync;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.Portal.Virtualization
{
    public abstract class DocumentBinaryProvider
    {
        protected const string DEFAULTBINARY_NAME = "Binary";

        // ============================================================ Provider 

        private static DocumentBinaryProvider _current;

        public static DocumentBinaryProvider Current
        {
            get
            {
                if (_current == null)
                {
                    var baseType = typeof(DocumentBinaryProvider);
                    var defType = typeof(DefaultDocumentBinaryProvider);
                    var dbpType = TypeResolver.GetTypesByBaseType(baseType).FirstOrDefault(t =>
                        string.Compare(t.FullName, baseType.FullName, StringComparison.InvariantCultureIgnoreCase) != 0 &&
                        string.Compare(t.FullName, defType.FullName, StringComparison.InvariantCultureIgnoreCase) != 0) ?? defType;

                    _current = Activator.CreateInstance(dbpType) as DocumentBinaryProvider;

                    if (_current == null)
                        SnLog.WriteInformation("DocumentBinaryProvider not present.");
                    else
                        SnLog.WriteInformation("DocumentBinaryProvider created: " + _current.GetType().FullName);
                }

                return _current;
            }
        }

        // ============================================================ Instance API

        public abstract Stream GetStream(Node node, string propertyName, out string contentType, out BinaryFileName fileName);
        public abstract BinaryFileName GetFileName(Node node, string propertyName = DEFAULTBINARY_NAME);
    }

    public class DefaultDocumentBinaryProvider : DocumentBinaryProvider
    {
        public override Stream GetStream(Node node, string propertyName, out string contentType, out BinaryFileName fileName)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            var binaryData = GetBinaryData(node, propertyName);
            if (binaryData != null)
            {
                contentType = binaryData.ContentType;

                // default property switch
                fileName = IsDefaultProperty(node, propertyName) ? new BinaryFileName(node.Name) : binaryData.FileName;

                // feature: AD sync
                var adSettings = node as ADSettings;
                if (adSettings != null && !adSettings.IncludePasswords())
                {
                    // return a stream that contains generic GUID values instead of the encoded passwords
                    return adSettings.RemovePasswords(binaryData.GetStream());
                }

                return binaryData.GetStream();
            }

            contentType = string.Empty;
            fileName = string.Empty;

            return null;
        }

        public override BinaryFileName GetFileName(Node node, string propertyName = DEFAULTBINARY_NAME)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            // default binary property switch
            if (IsDefaultProperty(node, propertyName))
                return node.Name;

            var binaryData = GetBinaryData(node, propertyName);
            if (binaryData != null)
                return binaryData.FileName;

            return node.Name;
        }

        // ============================================================ Helper methods

        private static BinaryData GetBinaryData(Node node, string propertyName = DEFAULTBINARY_NAME)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            BinaryData binaryData = null;
            var content = Content.Create(node);

            // try to find a field with this name
            if (content.Fields.ContainsKey(propertyName) && content.Fields[propertyName] is BinaryField)
                binaryData = content[propertyName] as BinaryData;

            // no field found, try a property
            if (binaryData == null)
            {
                var property = node.PropertyTypes[propertyName];
                if (property != null && property.DataType == DataType.Binary)
                    binaryData = node.GetBinary(property);
            }

            return binaryData;
        }

        private static bool IsDefaultProperty(Node node, string propertyName)
        {
            // the Binary property, or empty
            return node is SenseNet.ContentRepository.File &&
                   (string.IsNullOrEmpty(propertyName) ||
                    string.Compare(propertyName, DEFAULTBINARY_NAME, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}

