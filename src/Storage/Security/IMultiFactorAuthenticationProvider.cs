namespace SenseNet.Storage.Security
{
    /// <summary>
    /// Defines methods for setting up multi-factor authentication and validating user input.
    /// </summary>
    public interface IMultiFactorAuthenticationProvider
    {
        /// <summary>
        /// Gets a name that identifies the application. This should be configured differently for different repositories.
        /// </summary>
        public string GetApplicationName();
        /// <summary>
        /// Generates QR code url and a manual entry key for two-factor authentication.
        /// </summary>
        /// <param name="appName">Application name</param>
        /// <param name="userName">Unique user identifier</param>
        /// <param name="key">Secret key for the user</param>
        /// <returns>A url that represent a QR code and an alternative entry key for manual setup.</returns>
        public (string Url, string EntryKey) GenerateSetupCode(string appName, string userName, string key);
        /// <summary>
        /// Validates user input for two-factor authentication.
        /// </summary>
        /// <param name="key">Secret key for the user</param>
        /// <param name="codeToValidate">Input code provided by the user</param>
        /// <returns>True if the input matches the current one-time code.</returns>
        public bool ValidateTwoFactorCode(string key, string codeToValidate);
    }
}
