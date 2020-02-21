using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core
{
    public static class IdentityExtensions
    {
        /// <summary>
        /// Adds the Authentication middleware and a middleware for setting the sensenet current user.
        /// </summary>
        /// <param name="app">The Microsoft.AspNetCore.Builder.IApplicationBuilder to add the middleware to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseSenseNetAuthentication(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseSenseNetUser();

            return app;
        }
        /// <summary>
        /// Adds a middleware for setting the sensenet current user.
        /// </summary>
        /// <param name="app">The Microsoft.AspNetCore.Builder.IApplicationBuilder to add the middleware to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseSenseNetUser(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                // At this point the user is already authenticated, which means
                // we can trust the information in the identity: we load the
                // user in elevated mode (using system account).

                var identity = context?.User?.Identity;
                User user = null;

                // Currently if the Name property is filled, we try to load users based only
                // on that value, ignoring the sub claim.

                if (!string.IsNullOrEmpty(identity?.Name))
                {
                    // first look for users by their login name
                    user = SystemAccount.Execute(() => User.Load(identity.Name));

                    if (user == null)
                    {
                        SnTrace.Security.Write("Unknown user: {0}", identity.Name);
                    }
                }
                else
                {
                    // Check if there is a sub claim. Look for sub by its simple name or
                    // using the longer name defined by the schema below.
                    var sub = context?.User?.Claims.FirstOrDefault(c =>
                        c.Type == "sub" || c.Properties.Any(p =>
                            string.Equals(p.Key,
                                "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/ShortTypeName",
                                StringComparison.InvariantCultureIgnoreCase) &&
                            p.Value == "sub"))?.Value;
                    if (!string.IsNullOrEmpty(sub))
                    {
                        // try to recognize sub as a user id or username
                        user = SystemAccount.Execute(() =>
                            int.TryParse(sub, out var subId) ? Node.Load<User>(subId) : User.Load(sub));
                    }
                }

                User.Current = user ?? User.Visitor;

                await next.Invoke();
            });

            return app;
        }
    }
}