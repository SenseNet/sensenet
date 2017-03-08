using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Security.ADSync
{
    public abstract class DirectoryProvider
    {
        /* ============================================================================== Static Access */
        private static DirectoryProvider _current;
        private static readonly object _lock = new object();
        private static bool _isInitialized;

        public static DirectoryProvider Current
        {
            get
            {
                if ((_current == null) && (!_isInitialized))
                {
                    lock (_lock)
                    {
                        if ((_current == null) && (!_isInitialized))
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(Providers.DirectoryProviderClassName))
                                    _current = (DirectoryProvider)TypeResolver.CreateInstance(Providers.DirectoryProviderClassName);
                            }
                            catch (TypeNotFoundException) // rethrow
                            {
                                throw new ConfigurationErrorsException(string.Concat(SR.Exceptions.Configuration.Msg_DirectoryProviderImplementationDoesNotExist, ": ", Providers.DirectoryProviderClassName));
                            }
                            catch (InvalidCastException) // rethrow
                            {
                                throw new ConfigurationErrorsException(string.Concat(SR.Exceptions.Configuration.Msg_InvalidDirectoryProviderImplementation, ": ", Providers.DirectoryProviderClassName));
                            }
                            finally
                            {
                                _isInitialized = true;
                            }

                            if(_current == null)
                                SnLog.WriteInformation("DirectoryProvider not present.");
                            else
                                SnLog.WriteInformation("DirectoryProvider created: " + _current.GetType().FullName);
                        }
                    }
                }
                return _current;
            }
        }

        /* ============================================================================== Generic DirectoryServices Logic */
        public abstract void CreateNewADUser(User user, string passwd);
        public abstract void UpdateADUser(User user, string newPath, string passwd);
        public abstract void CreateNewADContainer(Node node);
        public abstract void UpdateADContainer(Node node, string newPath);
        public abstract void DeleteADObject(Node node);
        public abstract bool AllowMoveADObject(Node node, string newPath);
        public abstract bool IsADAuthenticated(string domain, string username, string pwd);
        public abstract bool SyncVirtualUserFromAD(string domain, string username, User virtualUser);
        public abstract bool SyncVirtualUserFromAD(Guid guid, User virtualUser);
        public abstract bool IsADAuthEnabled(string domain);
        public abstract bool IsVirtualADUserEnabled(string domain);
    }
}
