using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.Core.Authentication;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    internal static class IdentityConstants
    {
        /// <summary>
        /// Base value for cookie expiration
        /// The NumericDate value is defined in the following document:
        /// https://tools.ietf.org/html/rfc7519
        /// </summary>
        internal static readonly DateTime JwtCookieExpirationBaseDate =
            DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

        internal const string JwtCookieName = ".sn.auth";
        internal const string HeaderAuthorization = "Authorization";
        internal const string HeaderBearerPrefix = "Bearer ";

        internal static readonly string[] ClaimIdentifiers = { "sub", "client_sub" };
    }

    public static class IdentityExtensions
    {
        /// <summary>
        /// Configures sensenet Authentication. Calling this service method is optional if
        /// you do not want to add custom configuration.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configure">Configuration method for authentication options.</param>
        public static IServiceCollection AddSenseNetAuthentication(this IServiceCollection services, Action<AuthenticationOptions> configure)
        {
            if (configure != null)
                services.Configure(configure);

            return services;
        }

        /// <summary>
        /// Adds the Asp.Net Authentication middleware and a middleware for setting the sensenet current user.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseSenseNetAuthentication(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetService<IOptions<AuthenticationOptions>>()?.Value;

            // add the optional cookie reader middleware - default is false
            if (options?.AddJwtCookie ?? false)
                app.UseSenseNetJwtCookieReader();

            app.UseAuthentication();
            app.UseSenseNetUser();

            // add the optional cookie writer middleware - default is false
            if (options?.AddJwtCookie ?? false)
                app.UseSenseNetJwtCookieWriter();

            return app;
        }

        /// <summary>
        /// Adds a middleware for setting the sensenet current user.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseSenseNetUser(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                var identity = context?.User?.Identity;
                User user = null;

                // At this point the user is already authenticated, which means
                // we can trust the information in the identity: we load the
                // user in elevated mode (using system account).
                if (identity?.IsAuthenticated ?? false)
                {
                    var options = context.RequestServices.GetService<IOptions<AuthenticationOptions>>()?.Value;

                    // if the caller provided a custom user loader method
                    if (options?.FindUserAsync != null)
                    {
                        user = await options.FindUserAsync(context.User).ConfigureAwait(false);
                    }
                    else
                    { 
                        // Check if there is a sub claim. Look for sub by its simple name or
                        // using the longer name defined by the schema below.
                        var sub = context.User.FindFirst(c =>
                            IdentityConstants.ClaimIdentifiers.Contains(c.Type) ||
                            c.Properties.Any(p =>
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

                    // make sure we do not set the user if it is not enabled
                    if (user != null && !user.Enabled)
                        user = null;
                }

                User.Current = user ?? User.DefaultUser;

                if (next != null)
                    await next();
            });

            return app;
        }
        
        /// <summary>
        /// Adds a middleware for reading the sensenet authentication cookie and setting
        /// the value in the Authorization header. Use this in conjunction with the
        /// <see cref="UseSenseNetJwtCookieWriter"/> configuration method.
        /// </summary>
        /// <remarks>This method must be called before UseAuthentication.</remarks>
        /// <param name="app">The Microsoft.AspNetCore.Builder.IApplicationBuilder to add the middleware to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseSenseNetJwtCookieReader(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                var authHeader = context.Request.Headers[IdentityConstants.HeaderAuthorization].FirstOrDefault();

                // If the client sent a token in the header the regular way,
                // this middleware will not modify that.

                if (string.IsNullOrEmpty(authHeader) && 
                    context.Request.Cookies.TryGetValue(IdentityConstants.JwtCookieName, out var authCookie))
                {
                    if (authCookie?.StartsWith(IdentityConstants.HeaderBearerPrefix) ?? false)
                        context.Request.Headers[IdentityConstants.HeaderAuthorization] = authCookie;
                }

                if (next != null)
                    await next();
            });

            return app;
        }
        /// <summary>
        /// Adds a middleware for setting the sensenet authentication cookie if it was
        /// provided by the client in the Authorization header. Use this in conjunction
        /// with the <see cref="UseSenseNetJwtCookieReader"/> configuration method.
        /// </summary>
        /// <remarks>This method must be called after UseAuthentication.</remarks>
        /// <param name="app">The Microsoft.AspNetCore.Builder.IApplicationBuilder to add the middleware to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseSenseNetJwtCookieWriter(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                // If the user is authenticated and there is an authorization header,
                // set a cookie containing the token. The expiration date should be
                // the same value as in the token - available as the 'exp' claim.

                if (context.User.Identity?.IsAuthenticated ?? false)
                {
                    var authHeader = context.Request.Headers[IdentityConstants.HeaderAuthorization].FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        // The 'exp' claim value is an integer that defines the number of
                        // seconds that have passed since the base date (1970.01.01 UTC).
                        // If the claim does not exist or cannot be parsed, set a default
                        // value of 5 minutes.

                        var expClaim = context.User.Claims.FirstOrDefault(cl => cl.Type == "exp")?.Value;
                        var expDate = !string.IsNullOrEmpty(expClaim) && long.TryParse(expClaim, out var expSeconds)
                            ? IdentityConstants.JwtCookieExpirationBaseDate.AddSeconds(expSeconds)
                            : DateTime.UtcNow.AddMinutes(5);

                        context.Response.Cookies.Append(IdentityConstants.JwtCookieName, authHeader,
                            new CookieOptions
                            {
                                IsEssential = true,
                                Secure = true,
                                HttpOnly = true,
                                SameSite = SameSiteMode.None,
                                Expires = expDate
                            });
                    }
                }
                else
                {
                    context.Response.Cookies.Delete(IdentityConstants.JwtCookieName);
                }

                if (next != null)
                    await next();
            });

            return app;
        }
    }
}