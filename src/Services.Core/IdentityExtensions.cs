using Microsoft.AspNetCore.Builder;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;

namespace SenseNet.Services.Core
{
    public static class IdentityExtensions
    {
        public static IApplicationBuilder UseSenseNetAuthentication(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseSenseNetUser();

            return app;
        }
        public static IApplicationBuilder UseSenseNetUser(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                // At this point the user is already authenticated, which means
                // we can trust the information in the identity: we load the
                // user in elevated mode (using system account).

                var identity = context?.User?.Identity;
                if (!string.IsNullOrEmpty(identity?.Name))
                {
                    // Currently we look for users by their login name. In the future we may add
                    // a more flexible user discovery algorithm here.

                    var user = SystemAccount.Execute(() => User.Load(identity.Name));
                    if (user != null)
                    {
                        User.Current = user;
                    }
                    else
                    {
                        SnTrace.Security.Write("Unknown user: {0}", identity.Name);
                    }
                }

                await next.Invoke();
            });

            return app;
        }
    }
}
