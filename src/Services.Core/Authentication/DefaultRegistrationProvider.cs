﻿using Microsoft.AspNetCore.Http;
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
    /// Use the <see cref="AuthenticationExtensions.AddProvider"/> method during
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

            // content name: email
            var user = await CreateUser(email, email, email, fullName, u =>
            {
                u["ExternalUserProviders"] = external;
            }, cancellationToken);

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
    }
}
