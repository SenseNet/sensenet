using System;
using Google.Authenticator;
using Microsoft.Extensions.Options;
using SenseNet.Client;
using SenseNet.Common;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.Storage.Security;

namespace SenseNet.ContentRepository.Security.MultiFactor
{
    internal class DefaultMultiFactorProvider : IMultiFactorAuthenticationProvider
    {
        private readonly MultiFactorOptions _multiFactorOptions;
        private readonly ClientStoreOptions _clientStoreOptions;

        public DefaultMultiFactorProvider(IOptions<ClientStoreOptions> clientStoreOptions, IOptions<MultiFactorOptions> multiFactorOptions)
        {
            _multiFactorOptions = multiFactorOptions.Value;
            _clientStoreOptions = clientStoreOptions.Value;
        }

        public string GetApplicationName()
        {
            // if app name is configured
            if (!string.IsNullOrEmpty(_multiFactorOptions.ApplicationName))
                return _multiFactorOptions.ApplicationName;

            // fallback to the url
            return string.IsNullOrEmpty(_clientStoreOptions.RepositoryUrl)
                ? "sensenet"
                : _clientStoreOptions.RepositoryUrl.TrimSchema().Replace(":", string.Empty);
        }

        public (string Url, string EntryKey) GenerateSetupCode(string userAccount, string key)
        {
            if (string.IsNullOrEmpty(userAccount))
                throw new ArgumentNullException(nameof(userAccount));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var tfa = new TwoFactorAuthenticator();

            var setupInfo = tfa.GenerateSetupCode(GetApplicationName(), userAccount,
                key.Truncate(GetMaxKeyLength()), false, GetPixelsPerModule());

            var qrCodeImageUrl = setupInfo.QrCodeSetupImageUrl;
            var manualEntrySetupCode = setupInfo.ManualEntryKey;

            return (qrCodeImageUrl, manualEntrySetupCode);
        }

        public bool ValidateTwoFactorCode(string key, string codeToValidate)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(codeToValidate))
                throw new ArgumentNullException(nameof(codeToValidate));

            var tfa = new TwoFactorAuthenticator();
            return tfa.ValidateTwoFactorPIN(key.Truncate(GetMaxKeyLength()), codeToValidate,
                TimeSpan.FromMinutes(GetTimeTolerance()));
        }

        // value must be between 20 and 100
        private int GetMaxKeyLength() => Math.Min(100, Math.Max(20, _multiFactorOptions.MaxKeyLength));

        // value must be between 2 and 10
        private int GetPixelsPerModule() => Math.Min(10, Math.Max(2, _multiFactorOptions.PixelsPerModule));

        // value must be between 1 and 10
        private int GetTimeTolerance() => Math.Min(10, Math.Max(1, _multiFactorOptions.TimeToleranceMinutes));
    }
}
