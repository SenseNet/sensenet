using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Storage.Security
{
    /// <summary>
    /// Provides a password salt by any algorithm.
    /// </summary>
    public interface IPasswordSaltProvider
    {
        /// <summary>
        /// Returns with a password salt.
        /// </summary>
        string GetPasswordSalt();
    }

    public abstract class PasswordHashProvider
    {
        private static PasswordHashProvider __instance;
        private static object _lock = new object();
        private static PasswordHashProvider Instance
        {
            get
            {
                if (__instance == null)
                {
                    lock (_lock)
                    {
                        if (__instance == null)
                        {
                            try
                            {
                                __instance = (PasswordHashProvider)TypeResolver.CreateInstance(Providers.PasswordHashProviderClassName);
                            }
                            catch (TypeNotFoundException) // rethrow
                            {
                                throw new ConfigurationException(String.Concat(SR.Exceptions.Configuration.Msg_PasswordHashProviderImplementationDoesNotExist, ": ", Providers.PasswordHashProviderClassName));
                            }
                            catch (InvalidCastException) // rethrow
                            {
                                throw new ConfigurationException(String.Concat(SR.Exceptions.Configuration.Msg_InvalidPasswordHashProviderImplementation, ": ", Providers.PasswordHashProviderClassName));
                            }
                            SnLog.WriteInformation("PasswordHashProvider created: " + __instance);
                        }
                    }
                }
                return __instance;
            }
        }

        /// <summary>
        /// Generates a hash from the given password with the saltProvider if there is.
        /// </summary>
        public static string EncodePassword(string passwordInClearText, IPasswordSaltProvider saltProvider)
        {
            return Instance.Encode(passwordInClearText, saltProvider);
        }
        /// <summary>
        /// Checks the password by the given hash and saltProvider with the configured or default PasswordHashProvider.
        /// According to configuration does the migration too.
        /// </summary>
        public static bool CheckPassword(string passwordInClearText, string hash, IPasswordSaltProvider saltProvider)
        {
            return Instance.Check(passwordInClearText, hash, saltProvider);
        }

        /// <summary>
        /// Implementation of the hash generator. Uses the saltProvider if there is.
        /// </summary>
        protected abstract string Encode(string text, IPasswordSaltProvider saltProvider);
        /// <summary>
        /// Implementation of the checking of the password-hash match. Uses the saltProvider if there is.
        /// </summary>
        protected abstract bool Check(string text, string hash, IPasswordSaltProvider saltProvider);

        protected string GenerateSalt(IPasswordSaltProvider saltProvider)
        {
            if (saltProvider == null)
                return string.Empty;
            return saltProvider.GetPasswordSalt();
        }

        // ======================== Migration

        private static bool __outdatedInstanceResolved;
        private static PasswordHashProvider __outdatedInstance;
        private static PasswordHashProvider OutdatedInstance
        {
            get
            {
                if (!__outdatedInstanceResolved)
                {
                    lock (_lock)
                    {
                        if (!__outdatedInstanceResolved)
                        {
                            if (Configuration.Security.EnablePasswordHashMigration)
                            {
                                try
                                {
                                    __outdatedInstance = (PasswordHashProvider)TypeResolver.CreateInstance(Providers.OutdatedPasswordHashProviderClassName);
                                }
                                catch (TypeNotFoundException) // rethrow
                                {
                                    throw new ConfigurationException(String.Concat(SR.Exceptions.Configuration.Msg_PasswordHashProviderImplementationDoesNotExist, ": ", Providers.OutdatedPasswordHashProviderClassName));
                                }
                                catch (InvalidCastException) // rethrow
                                {
                                    throw new ConfigurationException(String.Concat(SR.Exceptions.Configuration.Msg_InvalidPasswordHashProviderImplementation, ": ", Providers.OutdatedPasswordHashProviderClassName));
                                }
                                SnLog.WriteInformation("PasswordHashProvider created for migration: " + __outdatedInstance);
                            }
                            __outdatedInstanceResolved = true;
                        }
                    }
                }
                return __outdatedInstance;
            }
        }

        /// <summary>
        /// Checks the password by the given hash and saltProvider with the configured or default OutdatedPasswordHashProvider.
        /// </summary>
        public static bool CheckPasswordForMigration(string passwordInClearText, string hash, IPasswordSaltProvider saltProvider)
        {
            return OutdatedInstance.Check(passwordInClearText, hash, saltProvider);
        }
   }
}
