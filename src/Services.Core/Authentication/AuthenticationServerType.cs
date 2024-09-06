namespace SenseNet.Services.Core.Authentication
{
    /// <summary>
    /// Defines which authentication server type the repository app uses.
    /// </summary>
    public enum AuthenticationServerType
    {
        /// <summary>
        /// IdentityServer4
        /// </summary>
        IdentityServer = 0,
        /// <summary>
        /// Simple sensenet authentication, mostly for smaller projects
        /// </summary>
        SNAuth = 1,
    }
}
