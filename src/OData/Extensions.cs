using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.OData
{
    public static class Extensions
    {
        /// <summary>
        /// Registers the sensenet OData middleware in the pipeline
        /// if the request contains the odata.svc prefix.
        /// </summary>
        /// <param name="builder">IApplicationBuilder instance.</param>
        /// <param name="buildAppBranch">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline after the sensenet OData middleware.</param>
        public static IApplicationBuilder UseSenseNetOdata(this IApplicationBuilder builder, 
            Action<IApplicationBuilder> buildAppBranch = null)
        {
            // add OData middleware only if the request contains the appropriate prefix
            builder.MapWhen(httpContext => httpContext.Request.Path.StartsWithSegments("/odata.svc"),
                appBranch =>
                {
                    appBranch.UseMiddleware<ODataMiddleware>();

                    buildAppBranch?.Invoke(appBranch);
                });

            return builder;
        }

        public static void SetODataRequest(this HttpContext httpContext, ODataRequest odataRequest)
        {
            httpContext.Items[ODataMiddleware.ODataRequestHttpContextKey] = odataRequest;
        }
        public static ODataRequest GetODataRequest(this HttpContext httpContext)
        {
            return httpContext.Items[ODataMiddleware.ODataRequestHttpContextKey] as ODataRequest;
        }

        /// <summary>
        /// Adds an <see cref="IOperationMethodExecutionPolicy"/> implementation instance to the active policies.
        /// Uses the Name property of the instance.
        /// </summary>
        /// <param name="builder">The actual <see cref="RepositoryBuilder"/> instance.</param>
        /// <param name="policy">An <see cref="IOperationMethodExecutionPolicy"/> implementation instance.</param>
        /// <returns>The given builder.</returns>
        public static RepositoryBuilder UseOperationMethodExecutionPolicy(this RepositoryBuilder builder,
            IOperationMethodExecutionPolicy policy)
        {
            OperationCenter.Policies.Add(policy.Name, policy);
            return builder;
        }
        /// <summary>
        /// Adds an <see cref="IOperationMethodExecutionPolicy"/> implementation instance to the active policies.
        /// This method renames the policy (not uses the Name property of the instance).
        /// </summary>
        /// <param name="builder">The actual <see cref="RepositoryBuilder"/> instance.</param>
        /// <param name="name">New name of the policy.</param>
        /// <param name="policy">An <see cref="IOperationMethodExecutionPolicy"/> implementation instance.</param>
        /// <returns>The given builder.</returns>
        public static RepositoryBuilder UseOperationMethodExecutionPolicy(this RepositoryBuilder builder,
            string name, IOperationMethodExecutionPolicy policy)
        {
            OperationCenter.Policies.Add(name, policy);
            return builder;
        }
        /// <summary>
        /// Adds an inline Func&lt;IUser, OperationCallingContext, bool&gt; as OperationMethod execution policy.
        /// </summary>
        /// <param name="builder">The actual <see cref="RepositoryBuilder"/> instance.</param>
        /// <param name="name">Name of the policy.</param>
        /// <param name="policyFunction"></param>
        /// <returns>The given builder.</returns>
        public static RepositoryBuilder UseOperationMethodExecutionPolicy(this RepositoryBuilder builder,
            string name, Func<IUser, OperationCallingContext, bool> policyFunction)
        {
            OperationCenter.Policies.Add(name, new InlineOperationMethodExecutionPolicy(name, policyFunction));
            return builder;
        }

    }
}
