using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Services.Core.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

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
        public string DefaultParentPath { get; protected internal set; }
        public string DefaultUserType { get; protected internal set; }
        public ICollection<string> DefaultGroups { get; protected internal set; }

        public Task<User> CreateLocalUserAsync(Content content, HttpContext context, string userName, string password, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<User> CreateProviderUserAsync(Content content, HttpContext context, string provider, string userId, 
            ClaimInfo[] claims, CancellationToken cancellationToken)
        {
            var parentPath = string.IsNullOrEmpty(DefaultParentPath) ? "/Root/IMS/Public" : DefaultParentPath;
            var userType = string.IsNullOrEmpty(DefaultUserType) ? "User" : DefaultUserType;

            var parent = RepositoryTools.CreateStructure(parentPath, "Domain") ??
                await Content.LoadAsync(parentPath, cancellationToken).ConfigureAwait(false);

            var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;

            // Fill the field with an initial JSON data containing a single provider data.
            // Later, if the user already exists, this field needs to be edited: the new id
            // should be inserted instead of overwriting the current value.
            var external = $"{{ \"{provider}\": {{ \"Id\": \"{userId}\", \"Completed\": false }} }}";

            // User content name will be the email.
            // Current behavior: if a user with the same name exists, make sure we create 
            // a new one and let the application deal with merging them later.
            var name = ContentNamingProvider.GetNameFromDisplayName(email);
            if (Node.Exists(RepositoryPath.Combine(parent.Path, name)))
                name = ContentNamingProvider.IncrementNameSuffixToLastName(name, parent.Id);
            
            var user = Content.CreateNew(userType, parent.ContentHandler, name);
            user["ExternalUserProviders"] = external;
            user["Email"] = email;
            user["FullName"] = fullName;
            user.DisplayName = fullName;

            user.Save();

            await AddUserToDefaultGroupsAsync(user.ContentHandler as User, cancellationToken).ConfigureAwait(false);

            return user.ContentHandler as User;
        }

        protected async System.Threading.Tasks.Task AddUserToDefaultGroupsAsync(User user, CancellationToken cancellationToken)
        {
            if (DefaultGroups == null)            
                return;
            
            var errorGroups = new List<string>();
            Exception firstException = null;

            foreach (var groupIdOrPath in DefaultGroups.Where(gr => !string.IsNullOrEmpty(gr)))
            {
                try
                {
                    var group = await Node.LoadNodeByIdOrPathAsync(groupIdOrPath, cancellationToken).ConfigureAwait(false) as Group;
                    group?.AddMember(user as User);
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
    }
}
