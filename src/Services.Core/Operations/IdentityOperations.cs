using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Email;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Services.Core.Authentication;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Operations
{
    public class ClaimInfo
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }

    [Serializable]
    public class MissingDomainException : Exception
    {
        public MissingDomainException() { }
        public MissingDomainException(string message) : base(message) { }
        public MissingDomainException(string message, Exception inner) : base(message, inner) { }
        protected MissingDomainException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public static class IdentityOperations
    {
        /// <summary>Validates the provided user credentials.</summary>
        /// <snCategory>Authentication</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="userName">Username (domain name can be omitted if it is the default).</param>
        /// <param name="password">Password</param>
        /// <returns>A custom object containing basic user data. If the credentials are not valid,
        /// the request throws a <see cref="SenseNetSecurityException"/> and return a 404 response.
        /// </returns>
        /// <example>
        /// <code>
        /// {
        ///     id: 1234,
        ///     email: "mail@example.com",
        ///     username: "example",
        ///     name: "example",
        ///     loginName: "example"
        /// }
        /// </code>
        /// </example>
        /// <exception cref="SenseNetSecurityException">Thrown when login is unsuccessful.</exception>
        /// <exception cref="MissingDomainException">Thrown when the domain is missing but the login algorithm needs it.</exception>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.All)]
        public static CredentialValidationResult ValidateCredentials(Content content, HttpContext context, string userName, string password)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (userName == null)
                throw new ArgumentNullException(nameof(userName));
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            // check user in elevated mode, because this is a system operation
            using (new SystemAccount())
            {
                var user = User.Load(userName);
                if (user == null)
                {
                    SnTrace.Security.Write($"Could not find a user with the name: {userName}");
                }
                else if (!user.Enabled)
                {
                    SnTrace.Security.Write($"User {userName} is disabled, not allowed to log in.");
                    user = null;
                }

                if (user != null)
                {
                    if (user.CheckPasswordMatch(password))
                    {
                        return new CredentialValidationResult
                        {
                            Id = user.Id,
                            Email = user.Email,
                            Username = user.Username,
                            Name = user.Name,
                            LoginName = user.LoginName
                        };
                    }

                    SnTrace.Security.Write($"Password match failed for user: {userName}");
                }
            }

            throw new SenseNetSecurityException("Invalid username or password.");
        }

        public class CredentialValidationResult
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            [JsonProperty("email")]
            public string Email { get; set; }
            [JsonProperty("username")]
            public string Username { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("loginName")]
            public string LoginName { get; set; }
        }

        /// <summary>Creates an external user who registered using one of the available
        /// external providers.</summary>
        /// <snCategory>Users and Groups</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="provider">Name of the provider (e.g. Google, GitHub).</param>
        /// <param name="userId">External user id given by the provider.</param>
        /// <param name="claims">List of claims given by the provider.</param>
        /// <returns>The newly created user content.</returns>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators)]
        public static async Task<Content> CreateUserByProvider(Content content, HttpContext context, string provider, 
            string userId, string claims)
        {
            if (!(context.RequestServices.GetService(typeof(RegistrationProviderStore)) is RegistrationProviderStore providerStore))
                throw new InvalidOperationException("sensenet user registration service is not available.");

            var registrationProvider = providerStore.Get(provider);
            var claimsList = string.IsNullOrEmpty(claims)
                ? Array.Empty<ClaimInfo>()
                : JsonConvert.DeserializeObject(claims, typeof(ClaimInfo[])) as ClaimInfo[];

            var user = await registrationProvider.CreateProviderUserAsync(content, context, provider, 
                userId, claimsList, context.RequestAborted).ConfigureAwait(false);

            return Content.Create(user);
        }

        /// <summary>Creates a local user who registered using a username and password.</summary>
        /// <snCategory>Users and Groups</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="loginName">Login name.</param>
        /// <param name="password">Password.</param>
        /// <param name="email">Email address.</param>
        /// <returns>The newly created user content.</returns>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators)]
        public static async Task<Content> CreateLocalUser(Content content, HttpContext context, string loginName, 
            string password, string email)
        {
            if (!(context.RequestServices.GetService(typeof(RegistrationProviderStore)) is RegistrationProviderStore providerStore))
                throw new InvalidOperationException("sensenet user registration service is not available.");

            var registrationProvider = providerStore.Get("local");
            var user = await registrationProvider.CreateLocalUserAsync(content, context, loginName,
                password, email, context.RequestAborted).ConfigureAwait(false);

            return Content.Create(user);
        }

        [ODataAction(OperationName = "SendChangePasswordMail", Icon = "security", DisplayName = "$Action,SendPasswordChange")]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.All)]
        public static async Task SendChangePasswordMailByEmail(Content content, HttpContext httpContext, 
            string email, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            // This method will never throw an exception (except if no email address was provided).
            // The caller can be a Visitor, so they should not receive any information about
            // whether the user exists or not and if we know the email or not.

            // query user in elevated mode
            using (new SystemAccount())
            {
                var user = Content.All.FirstOrDefault(c => c.TypeIs("User") && (string)c["Email"] == email);
                if (user == null)
                    return;

                try
                {
                    await SendChangePasswordMail(user, httpContext, returnUrl).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, $"Error during sending password change email for {email}.");
                }
            }
        }

        [ODataAction(Icon = "security", DisplayName = "$Action,SendPasswordChange")]
        [ContentTypes(N.CT.User)]
        [RequiredPermissions(N.P.Open)]
        public static async Task SendChangePasswordMail(Content content, HttpContext httpContext, string returnUrl = null)
        {
            var user = (User)content.ContentHandler;
            if (!PasswordIsEditableByCurrentUser(user))
                throw new SenseNetSecurityException("You do not have enough permissions " +
                                                    $"to change the password of {user.Username}.");

            // collect services
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<IEmailSender>>();
            var authOptions = httpContext.RequestServices.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
            var clientStoreOptions = httpContext.RequestServices.GetRequiredService<IOptions<ClientStoreOptions>>().Value;
            var templateManager = httpContext.RequestServices.GetRequiredService<IEmailTemplateManager>();
            var emailSender = httpContext.RequestServices.GetRequiredService<IEmailSender>();

            // default return url
            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = !string.IsNullOrEmpty(authOptions.ClientApplicationUrl)
                    ? authOptions.ClientApplicationUrl.AddUrlSchema()
                    : authOptions.Authority.AddUrlSchema();

            try
            {
                var email = (string)content["Email"];
                var fullName = (string)content["FullName"];

                if (string.IsNullOrEmpty(email))
                    throw new InvalidOperationException("Email address is empty.");

                // set a token on the user
                var guid = Guid.NewGuid().ToString();

                content["SyncGuid"] = guid;
                content.SaveSameVersion();

                logger.LogTrace($"Sync guid {guid} was set on user {content.Name} ({content.Id}) " +
                                "before sending password change mail.");

                var repositoryUrl = clientStoreOptions.RepositoryUrl.AddUrlSchema().TrimEnd('/');
                var encodedRepoUrl = System.Net.WebUtility.UrlEncode(repositoryUrl);

                // append additional parameters
                returnUrl = returnUrl
                    .AddUrlParameter("repoUrl", encodedRepoUrl)
                    .AddUrlParameter("snrepo", encodedRepoUrl);

                //TODO: let developers customize password change action link
                // Assemble the link the user will receive in the email. This will take them to the
                // IdentityServer UI to change their password.
                var actionUrl = $"{authOptions.Authority.AddUrlSchema().TrimEnd('/')}/account/PasswordChange?" +
                                $"returnUrl={HttpUtility.UrlEncode(returnUrl)}&token={guid}";

                logger.LogTrace($"Action url to include in password change email for user {content.Name} ({content.Id}): " +
                                actionUrl);

                var (subject, message) = await templateManager.LoadEmailTemplateAsync(
                        "Registration/ChangePassword",
                        p =>
                        {
                            p["Email"] = email;
                            p["Username"] = email;
                            p["FullName"] = fullName;
                            p["RepositoryUrl"] = repositoryUrl;
                            p["ActionUrl"] = actionUrl;

                            return Task.CompletedTask;
                        })
                    .ConfigureAwait(false);

                // send the email to the user
                await emailSender.SendAsync(new EmailData
                {
                    ToAddress = email,
                    ToName = fullName,
                    //FromName = 
                    Subject = subject,
                    Body = message
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SnLog.WriteException(ex, $"Error during sending password change mail. User: {content.Path} ({content.Id}). " +
                                           $"Authority: {authOptions.Authority}");

                throw new InvalidOperationException($"Sending password change mail failed. {ex.Message}");
            }
        }

        [ODataAction(Icon = "security", DisplayName = "$Action,PasswordChange")]
        [ContentTypes(N.CT.User)]
        [RequiredPermissions(N.P.Open)]
        [Scenario(N.S.ContextMenu)]
        public static void ChangePassword(Content content, string password)
        {
            //TODO: enforce password policy when available
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));
            
            var user = (User)content.ContentHandler;
            if (!PasswordIsEditableByCurrentUser(user))
                throw new SenseNetSecurityException($"You do not have enough permissions to change the password of {user.Username}.");

            if (!user.Enabled)
                throw new InvalidOperationException("You cannot change the password of an inactive user.");

            // this is executed in elevated mode so that users may change their own password
            using (new SystemAccount())
            {
                try
                {
                    content["Password"] = password;
                    content.SaveSameVersion();
                }
                catch (Exception ex)
                {
                    SnLog.WriteException(ex, $"Error during password change. User: {content.Path} ({content.Id}).");

                    throw new InvalidOperationException($"Password change failed. {ex.Message}");
                }
            }
        }

        private static bool PasswordIsEditableByCurrentUser(User user)
        {
            // Every user can edit their own password and admins may forcefully change others' password
            // if they have Save permission on the user content anyway.
            return User.Current.Id == user.Id ||
                   SenseNet.Configuration.Providers.Instance.SecurityHandler.HasPermission(user, PermissionType.Save);
        }
    }
}
