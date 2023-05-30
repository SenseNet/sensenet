using Google.Authenticator;
using SenseNet.Storage.Security;

namespace SenseNet.ContentRepository.Security
{
    internal class DefaultMultiFactorProvider : IMultiFactorAuthenticationProvider
    {
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
