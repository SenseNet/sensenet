using SenseNet.Diagnostics;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.ApplicationModel;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Security.Cryptography
{
    public interface ICryptoServiceProvider
    {
        string Encrypt(string plainText);
        string Decrypt(string encodedText);
    }

    public class CryptoServiceProvider
    {
        /// <summary>Encrypts a short text using the current crypto service provider.</summary>
        /// <snCategory>Security</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="text">The text to encrypt.</param>
        /// <returns>The encrypted text.</returns>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static string Encrypt(Content content, HttpContext context, string text)
        {
            try
            {
                var csp = context.RequestServices.GetService<ICryptoServiceProvider>();
                return csp.Encrypt(text);
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger<CryptoServiceProvider>>();
                logger.LogWarning(ex, $"Error when trying to encrypt a text. {ex.Message}");
            }

            throw new InvalidOperationException("Could not encrypt text.");
        }

        /// <summary>Decrypts a short encrypted text using the current crypto service provider.</summary>
        /// <snCategory>Security</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="text">The text to decrypt.</param>
        /// <returns>A clear text original value.</returns>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static string Decrypt(Content content, HttpContext context, string text)
        {
            try
            {
                var csp = context.RequestServices.GetService<ICryptoServiceProvider>();
                return csp.Decrypt(text);
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetService<ILogger<CryptoServiceProvider>>();
                logger.LogWarning(ex, $"Error when trying to decrypt a text. {ex.Message}");
            }

            throw new InvalidOperationException("Could not decrypt text.");
        }
    }

    /// <summary>
    /// Proved cryptographic services based on the RSACryptoServiceProvider class and an
    /// X509Certificate2 certificate stored in the current user store.
    /// </summary>
    public class DefaultCryptoServiceProvider : ICryptoServiceProvider
    {
        private readonly CryptographyOptions _options;

        public DefaultCryptoServiceProvider(IOptions<CryptographyOptions> options)
        {
            _options = options.Value;
        }
        // ================================================================================= Handle certificate

        private X509Certificate2 _certificate;
        private bool _certLoaded;
        private X509Certificate2 Certificate
        {
            get
            {
                if (_certificate == null && !_certLoaded)
                {
                    _certificate = LoadCertificate();
                    _certLoaded = true;
                }

                return _certificate;
            }
        }

        private X509Certificate2 LoadCertificate()
        {
            // try to load the certificate from the user store first than from the machine store
            var certificate =
                LoadCertificate(StoreLocation.CurrentUser) ??
                LoadCertificate(StoreLocation.LocalMachine);

            if (certificate == null)
                SnLog.WriteWarning("Could not load x509 certificate.");

            return certificate;
        }
        private X509Certificate2 LoadCertificate(StoreLocation storeLocation)
        {
            X509Store store = null;

            try
            {
                store = new X509Store(storeLocation);
                store.Open(OpenFlags.ReadOnly);

                // note that CompareOrdinal does not work on the Thumbprint property
                return store.Certificates.Cast<X509Certificate2>().FirstOrDefault(cert =>
                    string.Compare(cert.Thumbprint, _options.CertificateThumbprint, StringComparison.InvariantCultureIgnoreCase) == 0);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, $"Could not load x509 certificate from store location {storeLocation}.");
            }
            finally
            {
                store?.Close();
            }

            return null;
        }

        // ================================================================================= ICryptoServiceProvider implementation

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            var certificate = Certificate;
            if (certificate == null)
                throw new InvalidOperationException("Certificate is not present or invalid, could not encrypt value.");

            return Encrypt(certificate, plainText);
        }
        public string Decrypt(string encodedText)
        {
            if (string.IsNullOrEmpty(encodedText))
                return encodedText;

            var certificate = Certificate;
            if (certificate == null)
                throw new InvalidOperationException("Certificate is not present or invalid, could not decrypt value.");

            return Decrypt(certificate, encodedText);
        }

        // ================================================================================= Encrypt/Decrypt

        private static string Encrypt(X509Certificate2 x509, string stringToEncrypt)
        {
            if (x509 == null || string.IsNullOrEmpty(stringToEncrypt))
                throw new ArgumentException("An x509 certificate and string for encryption must be provided.");

            var rsa = (RSACryptoServiceProvider)x509.PublicKey.Key;
            byte[] bytestoEncrypt = Encoding.ASCII.GetBytes(stringToEncrypt);
            byte[] encryptedBytes = rsa.Encrypt(bytestoEncrypt, false);

            return Convert.ToBase64String(encryptedBytes);
        }
        private static string Decrypt(X509Certificate2 x509, string encryptedText)
        {
            if (x509 == null || string.IsNullOrEmpty(encryptedText))
                throw new ArgumentException("An x509 certificate and string for decryption must be provided.");

            if (!x509.HasPrivateKey)
                throw new InvalidOperationException("The x509 certificate does not contain a private key for decryption.");

            var rsa = (RSACryptoServiceProvider)x509.PrivateKey;
            byte[] bytestodecrypt = Convert.FromBase64String(encryptedText);
            byte[] plainbytes = rsa.Decrypt(bytestodecrypt, false);

            return Encoding.ASCII.GetString(plainbytes);
        }
    }
}
