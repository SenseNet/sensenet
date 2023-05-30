using Google.Authenticator;
using Microsoft.Extensions.Options;
using SenseNet.Client;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.Storage.Security;

namespace SenseNet.ContentRepository.Security.MultiFactor
{
    internal class DefaultMultiFactorProvider : IMultiFactorAuthenticationProvider
    {
        private readonly ClientStoreOptions _clientStoreOptions;

        public DefaultMultiFactorProvider(IOptions<ClientStoreOptions> clientStoreOptions)
        {
            _clientStoreOptions = clientStoreOptions.Value;
        }

        public string GetApplicationName()
        {
            //TODO: let operators configure app name independently
            return string.IsNullOrEmpty(_clientStoreOptions.RepositoryUrl)
                ? "sensenet"
                : _clientStoreOptions.RepositoryUrl.TrimSchema().Replace(":", string.Empty);
        }

        public (string Url, string EntryKey) GenerateSetupCode(string appName, string userAccount, string key)
        {
            var tfa = new TwoFactorAuthenticator();

            //TODO: get config values from an option object
            var setupInfo = tfa.GenerateSetupCode(appName, userAccount,
                key, false);

            var qrCodeImageUrl = setupInfo.QrCodeSetupImageUrl;
            var manualEntrySetupCode = setupInfo.ManualEntryKey;

            return (qrCodeImageUrl, manualEntrySetupCode);
        }

        public bool ValidateTwoFactorCode(string key, string codeToValidate)
        {
            var tfa = new TwoFactorAuthenticator();
            return tfa.ValidateTwoFactorPIN(key, codeToValidate);
        }
    }
}
