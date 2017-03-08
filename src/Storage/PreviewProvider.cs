using System;
using System.Configuration;
using SenseNet.Configuration;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage
{
    public interface IPreviewProvider
    {
        bool HasPreviewPermission(NodeHead nodeHead);
    }

    /// <summary>
    /// This internal class was created to make the DocumentPreviewProvider feature (that resides up in the ContentRepository layer) accessible here in the Storage layer.
    /// </summary>
    internal class PreviewProvider
    {
        // ============================================================================== Static internal API

        private static IPreviewProvider _previewProvider;
        private static readonly object _previewLock = new object();
        private static bool _isInitialized;

        /// <summary>
        /// Instance of a DocumentPreviewProvider in the Storage layer. This property is a duplicate of the Current property of the DocumentPreviewProvider class.
        /// </summary>
        private static IPreviewProvider Current
        {
            get
            {
                if ((_previewProvider == null) && (!_isInitialized))
                {
                    lock (_previewLock)
                    {
                        if ((_previewProvider == null) && (!_isInitialized))
                        {
                            try
                            {
                                _previewProvider = (IPreviewProvider)TypeResolver.CreateInstance(Providers.DocumentPreviewProviderClassName);
                            }
                            catch (TypeNotFoundException) // rethrow
                            {
                                throw new ConfigurationErrorsException(string.Concat(SR.Exceptions.Configuration.Msg_DocumentPreviewProviderImplementationDoesNotExist, ": ", Providers.DocumentPreviewProviderClassName));
                            }
                            catch (InvalidCastException) // rethrow
                            {
                                throw new ConfigurationErrorsException(string.Concat(SR.Exceptions.Configuration.Msg_InvalidDocumentPreviewProviderImplementation, ": ", Providers.DocumentPreviewProviderClassName));
                            }
                            finally
                            {
                                _isInitialized = true;
                            }

                            if (_previewProvider == null)
                                SnLog.WriteInformation("DocumentPreviewProvider not present.");
                            else
                                SnLog.WriteInformation("DocumentPreviewProvider created: " + _previewProvider.GetType().FullName);
                        }
                    }
                }
                return _previewProvider;
            }
        }

        internal static bool HasPreviewPermission(NodeHead nodeHead)
        {
            return Current != null && Current.HasPreviewPermission(nodeHead);
        }
    }
}
