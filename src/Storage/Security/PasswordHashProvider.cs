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

    public interface IPasswordHashProvider
    {
        /// <summary>
        /// Generates a hash from the given password with the saltProvider if there is.
        /// </summary>
        string Encode(string passwordInClearText, IPasswordSaltProvider saltProvider);
        /// <summary>
        /// Checks the password by the given hash and saltProvider with the configured or default PasswordHashProvider.
        /// According to configuration does the migration too.
        /// </summary>
        bool Check(string passwordInClearText, string hash, IPasswordSaltProvider saltProvider);
    }

    public interface IPasswordHashProviderForMigration : IPasswordHashProvider { }


    public abstract class PasswordHashProvider
    {
        private static IPasswordHashProvider Instance => Providers.Instance.PasswordHashProvider;

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
        public abstract string Encode(string text, IPasswordSaltProvider saltProvider);
        /// <summary>
        /// Implementation of the checking of the password-hash match. Uses the saltProvider if there is.
        /// </summary>
        public abstract bool Check(string text, string hash, IPasswordSaltProvider saltProvider);

        protected string GenerateSalt(IPasswordSaltProvider saltProvider)
        {
            if (saltProvider == null)
                return string.Empty;
            return saltProvider.GetPasswordSalt();
        }

        // ======================== Migration

        private static IPasswordHashProvider OutdatedInstance => Providers.Instance.PasswordHashProviderForMigration;

        /// <summary>
        /// Checks the password by the given hash and saltProvider with the configured or default OutdatedPasswordHashProvider.
        /// </summary>
        public static bool CheckPasswordForMigration(string passwordInClearText, string hash, IPasswordSaltProvider saltProvider)
        {
            return OutdatedInstance.Check(passwordInClearText, hash, saltProvider);
        }
   }
}
