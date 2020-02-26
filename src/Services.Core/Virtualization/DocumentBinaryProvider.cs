using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Security.ADSync;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Portal.Virtualization
{
    public abstract class DocumentBinaryProvider
    {
        protected const string DEFAULTBINARY_NAME = "Binary";

        // ============================================================ Provider 

        [Obsolete("Use Instance instead.")]
        public static DocumentBinaryProvider Current => Instance;

        internal const string DocumentBinaryProviderKey = "DocumentBinaryProvider";
        private static readonly object SyncRoot = new object();
        public static DocumentBinaryProvider Instance
        {
            get
            {
                var dbp = Providers.Instance.GetProvider<DocumentBinaryProvider>(DocumentBinaryProviderKey);
                if (dbp == null)
                {
                    lock (SyncRoot)
                    {
                        dbp = Providers.Instance.GetProvider<DocumentBinaryProvider>(DocumentBinaryProviderKey);
                        if (dbp != null) 
                            return dbp;

                        // set the default implementation
                        var defType = typeof(DefaultDocumentBinaryProvider);

                        try
                        {
                            dbp = Activator.CreateInstance(defType) as DocumentBinaryProvider;
                        }
                        catch (Exception ex)
                        {
                            SnLog.WriteException(ex, "Error during instantiating default document binary provider.");
                        }
                            
                        Providers.Instance.SetProvider(DocumentBinaryProviderKey, dbp);

                        if (dbp == null)
                            SnLog.WriteInformation("DocumentBinaryProvider not present.");
                        else
                            SnLog.WriteInformation("DocumentBinaryProvider created: " + defType.FullName);
                    }
                }

                return dbp;
            }
        }

        // ============================================================ Instance API

        public abstract Stream GetStream(Node node, string propertyName, HttpContext context, out string contentType, out BinaryFileName fileName);
        public abstract BinaryFileName GetFileName(Node node, string propertyName = DEFAULTBINARY_NAME);
    }

    public class DefaultDocumentBinaryProvider : DocumentBinaryProvider
    {
        public override Stream GetStream(Node node, string propertyName, HttpContext context, out string contentType, out BinaryFileName fileName)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var binaryData = GetBinaryData(node, propertyName);
            if (binaryData != null)
            {
                contentType = binaryData.ContentType;

                // default property switch
                fileName = IsDefaultProperty(node, propertyName) ? new BinaryFileName(node.Name) : binaryData.FileName;

                // feature: AD sync
                if (node is ADSettings adSettings && RemoveAdPasswords(adSettings, context))
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
                throw new ArgumentNullException(nameof(node));

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
                throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrEmpty(propertyName))
                propertyName = DEFAULTBINARY_NAME;

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
            return node is ContentRepository.File &&
                   (string.IsNullOrEmpty(propertyName) ||
                    string.Compare(propertyName, DEFAULTBINARY_NAME, StringComparison.OrdinalIgnoreCase) == 0);
        }

        protected static bool RemoveAdPasswords(ADSettings adSettings, HttpContext context)
        {
            if (adSettings == null)
                return false;

            // Save permission is needed for this setting to be able to see even the encrypted values
            if (!adSettings.Security.HasPermission(PermissionType.Save))
                return true;

            // in case of export or other special scenario, include the stored values
            if (context == null)
                return false;

            var includePassStr = context.Request.Query["includepasswords"];
            if (string.IsNullOrEmpty(includePassStr))
                return true;

            if (bool.TryParse(includePassStr, out var includePass))
                return !includePass;

            return true;
        }
    }
}

