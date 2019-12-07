using SenseNet.Diagnostics;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SenseNet.ApplicationModel;
using SenseNet.Tools;

namespace SenseNet.ContentRepository.Security.Cryptography
{
    public interface ICryptoServiceProvider
    {
        string Encrypt(string plainText);
        string Decrypt(string encodedText);
    }

    public class CryptoServiceProvider
    {
        // ================================================================================= Internal API

        private static ICryptoServiceProvider _instance;
        private static ICryptoServiceProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    var baseType = typeof(ICryptoServiceProvider);
                    var defType = typeof(DefaultCryptoServiceProvider);
                    var cspType = TypeResolver.GetTypesByInterface(baseType).FirstOrDefault(t =>
                        string.Compare(t.FullName, defType.FullName, StringComparison.InvariantCultureIgnoreCase) != 0) ?? defType;

                    _instance = Activator.CreateInstance(cspType) as ICryptoServiceProvider;

                    if (_instance == null)
                        SnLog.WriteInformation("CryptoServiceProvider not present.");
                    else
                        SnLog.WriteInformation("CryptoServiceProvider created: " + _instance.GetType().FullName);
                }

                return _instance;
            }
        }

        // ================================================================================= Static API

        public static string Encrypt(string plainText)
        {
            return Instance.Encrypt(plainText);
        }
        public static string Decrypt(string encodedText)
        {
            return Instance.Decrypt(encodedText);
        }

        // ================================================================================= OData API

        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static string Encrypt(Content content, string text)
        {
            return Encrypt(text);
        }
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators, N.R.Developers)]
        public static string Decrypt(Content content, string text)
        {
            return Decrypt(text);
        }
    }

    /// <summary>
    /// Proved cryptographic services based on the RSACryptoServiceProvider class and an
    /// X509Certificate2 certificate stored in the current user store.
    /// </summary>
    public class DefaultCryptoServiceProvider : ICryptoServiceProvider
    {
        // ================================================================================= Handle certificate

        private static X509Certificate2 _certificate;
        private static bool _certLoaded;
        private static X509Certificate2 Certificate
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

        private static X509Certificate2 LoadCertificate()
        {
            // try to load the certificate from the user store first than from the machine store
            var certificate =
                LoadCertificate(StoreLocation.CurrentUser) ??
                LoadCertificate(StoreLocation.LocalMachine);

            if (certificate == null)
                SnLog.WriteWarning("Could not load x509 certificate.");

            return certificate;
        }
        private static X509Certificate2 LoadCertificate(StoreLocation storeLocation)
        {
            X509Store store = null;

            try
            {
                store = new X509Store(storeLocation);
                store.Open(OpenFlags.ReadOnly);

                // note that CompareOrdinal does not work on the Thumbprint property
                return store.Certificates.Cast<X509Certificate2>().FirstOrDefault(cert =>
                    string.Compare(cert.Thumbprint, Configuration.Cryptography.CertificateThumbprint, StringComparison.InvariantCultureIgnoreCase) == 0);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, $"Could not load x509 certificate from store location {storeLocation}.");
            }
            finally
            {
                if (store != null)
                    store.Close();
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
