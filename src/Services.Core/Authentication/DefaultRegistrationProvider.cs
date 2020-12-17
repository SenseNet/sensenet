using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Services.Core.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SenseNet.Configuration;
using SenseNet.Portal.Handlers;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Authentication
{
    /// <summary>
    /// Default implementation for the <see cref="IRegistrationProvider"/> interface.
    /// Provides a basic algorithm for creating new users and filling their
    /// most important fields - e.g. email or name.
    /// </summary>
    /// <remarks>
    /// Developers may inherit from this class and provide their own implementation
    /// using the helper methods of this built-in implementation.
    /// Use the AddSenseNetRegistration and AddProvider methods during
    /// application start to register a custom instance for a particular 
    /// authentication provider (e.g. Google or Facebook).
    /// </remarks>
    public class DefaultRegistrationProvider : IRegistrationProvider
    {
        public string Name => "default";
        private readonly RegistrationOptions _options;

        public DefaultRegistrationProvider(IOptions<RegistrationOptions> options)
        {
            _options = options?.Value ?? new RegistrationOptions();
        }

        public async Task<User> CreateLocalUserAsync(Content content, HttpContext context, string loginName, string password, 
            string email, CancellationToken cancellationToken)
        {
            // content name: login name, because the email may be empty
            return await CreateUser(loginName, loginName, email, loginName,
                user =>
                {
                    user["Password"] = password;
                }, cancellationToken);
        }

        public async Task<User> CreateProviderUserAsync(Content content, HttpContext context, string provider, string userId, 
            ClaimInfo[] claims, CancellationToken cancellationToken)
        {
            var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
            
            // Fill the field with an initial JSON data containing a single provider data.
            // Later, if the user already exists, this field needs to be edited: the new id
            // should be inserted instead of overwriting the current value.
            var external = $"{{ \"{provider}\": {{ \"Id\": \"{userId}\", \"Completed\": false }} }}";

            // check if there is an existing user with this email
            var user = GetUserByEmail(email);
            if (user != null)
            {
                // existing user: merge
                MergeExternalData(user, email, fullName, external, provider, userId);
            }
            else
            {
                // content name: email
                user = await CreateUser(email, email, email, fullName, u =>
                {
                    u["ExternalUserProviders"] = external;
                },
                cancellationToken);
            }

            // save user avatar
            var image = claims.FirstOrDefault(c => c.Type == "image")?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(image)) 
                return user;

            try
            {
                await SaveImageAsync(user, image).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var msg = $"Could not save the user's avatar during registration. {ex.Message} User: {userId}, provider: {provider}, image: {image}";
                SnTrace.ContentOperation.WriteError(msg);
            }

            return user;
        }

        protected async Task<User> CreateUser(string nameCandidate, string loginName, string email, string fullName,
            Action<Content> setProperties, CancellationToken cancellationToken)
        {
            var parentPath = string.IsNullOrEmpty(_options.ParentPath) ? "/Root/IMS/Public" : _options.ParentPath;
            var userType = string.IsNullOrEmpty(_options.UserType) ? "User" : _options.UserType;

            var parent = RepositoryTools.CreateStructure(parentPath, "Domain") ??
                         await Content.LoadAsync(parentPath, cancellationToken).ConfigureAwait(false);

            // If a user with the same name exists, make sure we create 
            // a new one and let the application deal with merging them later.
            var name = ContentNamingProvider.GetNameFromDisplayName(nameCandidate);
            if (Node.Exists(RepositoryPath.Combine(parent.Path, name)))
                name = ContentNamingProvider.IncrementNameSuffixToLastName(name, parent.Id);

            var user = Content.CreateNew(userType, parent.ContentHandler, name);
            user["LoginName"] = loginName;
            user["Email"] = email;
            user["FullName"] = string.IsNullOrEmpty(fullName) ? name : fullName;
            user.DisplayName = string.IsNullOrEmpty(fullName) ? name : fullName;
            user["Enabled"] = true;

            setProperties?.Invoke(user);

            user.Save();

            await AddUserToDefaultGroupsAsync(user.ContentHandler as User, cancellationToken).ConfigureAwait(false);

            return user.ContentHandler as User;
        }

        protected async Task AddUserToDefaultGroupsAsync(User user, CancellationToken cancellationToken)
        {
            if (_options.Groups == null || _options.Groups.Count == 0)
                return;
            
            var errorGroups = new List<string>();
            Exception firstException = null;

            foreach (var groupIdOrPath in _options.Groups.Where(gr => !string.IsNullOrEmpty(gr)))
            {
                try
                {
                    var group = await Node.LoadNodeByIdOrPathAsync(groupIdOrPath, cancellationToken).ConfigureAwait(false) as Group;
                    group?.AddMember(user);
                }
                catch (Exception ex)
                {
                    if (firstException == null)
                        firstException = ex;

                    errorGroups.Add(groupIdOrPath);
                }
            }

            if (errorGroups.Any())
            {
                SnLog.WriteException(firstException, $"Error in group membership set operation during external user registration.",
                        properties: new Dictionary<string, object>()
                        {
                                { "UserId", user.Id },
                                { "Groups", string.Join(", ", errorGroups) }
                        });
            }
        }

        private async Task SaveImageAsync(User user, string imageUrl)
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(imageUrl).ConfigureAwait(false);

            // Determine the image type based on the response header. The url
            // is not enough because in many cases it does not contain an extension.
            // An alternative is to determine the type based on the binary, but that
            // would mean loading the image and converting it in memory using
            // legacy .net APIs.
            var contentTypeHeader = response.Content.Headers.GetValues("Content-Type").FirstOrDefault() ?? string.Empty;
            var contentType = "png";
            if (contentTypeHeader.Contains("jpg") || contentTypeHeader.Contains("jpeg"))
                contentType = "jpg";
            else if (contentTypeHeader.Contains("gif"))
                contentType = "gif";

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var bd = UploadHelper.CreateBinaryData("avatar." + contentType, stream);
            user.SetBinary("ImageData", bd);
            user.Save(SavingMode.KeepVersion);
        }

        private User GetUserByEmail(string email)
        {
            return Content.All.FirstOrDefault(c =>
                c.InTree(RepositoryStructure.ImsFolderPath) &&
                c.TypeIs("User") &&
                (string)c["Email"] == email &&
                (string)c["LoginName"] == email)?.ContentHandler as User;
        }

        private void MergeExternalData(User user, string email, string fullName, string externalProviderData, 
            string provider, string userId)
        {
            var userContent = Content.Create(user);
            var currentExternalData = (string)userContent["ExternalUserProviders"];

            if (string.IsNullOrEmpty(currentExternalData))
            {
                // no previous external data, simply set it
                SaveNewExternalData(externalProviderData);
            }
            else
            {
                var currentExternalJObject = JsonConvert.DeserializeObject<JObject>(currentExternalData ?? string.Empty);
                if (currentExternalJObject.ContainsKey(provider))
                {
                    // provider exists in previous external data
                    var currentProviderId = currentExternalJObject[provider]["Id"]?.Value<string>() ?? string.Empty;
                    if (string.IsNullOrEmpty(currentProviderId))
                    {
                        // the id is empty for some reason, simply set it
                        currentExternalJObject[provider]["Id"] = userId;

                        SaveNewExternalData(currentExternalJObject.ToString());
                    }
                    else if (!string.Equals(currentProviderId, userId))
                    {
                        // This should not happen: we've found a user with the same email and provider
                        // but with a different user id.
                        SnLog.WriteWarning($"CreateProviderUser: found an existing user with a matching email but non-matching " +
                                           $"provider id. Provider: {provider} Email: {email} Current id: {currentProviderId}" +
                                           $"New id: {userId}");

                        throw new InvalidOperationException($"Ambiguous user id for user {email} and provider {provider}");
                    }

                    // otherwise: the correct provider and id is already set, move on silently
                }
                else
                {
                    // merge the new provider to the existing list
                    var newExternalJObject = JsonConvert.DeserializeObject<JObject>(externalProviderData);
                    currentExternalJObject.Merge(newExternalJObject);

                    SaveNewExternalData(currentExternalJObject.ToString());
                }
            }

            void SaveNewExternalData(string externalData)
            {
                userContent["ExternalUserProviders"] = externalData;
                if (!string.IsNullOrEmpty(fullName))
                    userContent["FullName"] = fullName;

                userContent.SaveSameVersion();
            }
        }
    }
}
