using Microsoft.AspNetCore.Builder;
using SenseNet.ContentRepository;

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
                //TODO:~ load the authenticated user from the context and set it in sensenet
                //var user = context.User;
                User.Current = User.Administrator;

                await next.Invoke();
            });

            return app;
        }
    }
}
