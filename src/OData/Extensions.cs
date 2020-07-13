using System;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Services.Core;
using Task = System.Threading.Tasks.Task;

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
            builder.MapMiddlewareWhen<ODataMiddleware>("/odata.svc", buildAppBranch);

            builder.UseOperationMethodExecutionPolicy(new VersioningOperationMethodPolicy());

            return builder;
        }

        /// <summary>
        /// Adds an <see cref="IOperationMethodPolicy"/> instance to the list of active policies.
        /// Uses the Name property of the instance.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>        
        /// <param name="policy">An <see cref="IOperationMethodPolicy"/> instance.</param>
        public static IApplicationBuilder UseOperationMethodExecutionPolicy(this IApplicationBuilder builder,
            IOperationMethodPolicy policy)
        {
            OperationCenter.Policies[policy.Name] = policy;
            return builder;
        }
        /// <summary>
        /// Adds an <see cref="IOperationMethodPolicy"/> implementation instance to the active policies.
        /// This method renames the policy (does not use the Name property of the instance).
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <param name="name">New name of the policy.</param>
        /// <param name="policy">An <see cref="IOperationMethodPolicy"/> instance.</param>
        public static IApplicationBuilder UseOperationMethodExecutionPolicy(this IApplicationBuilder builder,
            string name, IOperationMethodPolicy policy)
        {
            OperationCenter.Policies[name] = policy;
            return builder;
        }
        /// <summary>
        /// Adds an inline Func&lt;IUser, OperationCallingContext, bool&gt; as an OperationMethod execution policy.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <param name="name">Name of the policy.</param>
        /// <param name="policyFunction">The policy function to execute.</param>
        public static IApplicationBuilder UseOperationMethodExecutionPolicy(this IApplicationBuilder builder,
            string name, Func<IUser, OperationCallingContext, OperationMethodVisibility> policyFunction)
        {
            OperationCenter.Policies[name] = new InlineOperationMethodPolicy(name, policyFunction);
            return builder;
        }

        /// <summary>
        /// Adds the OData request object to the items of HttpContext.
        /// </summary>
        internal static void SetODataRequest(this HttpContext httpContext, ODataRequest odataRequest)
        {
            httpContext.Items[ODataMiddleware.ODataRequestHttpContextKey] = odataRequest;
        }
        /// <summary>
        /// Gets the OData request object from the Items collection.
        /// </summary>
        internal static ODataRequest GetODataRequest(this HttpContext httpContext)
        {
            return httpContext.Items[ODataMiddleware.ODataRequestHttpContextKey] as ODataRequest;
        }

        /// <summary>
        /// Adds an <see cref="IOperationMethodPolicy"/> instance to the list of active policies.
        /// Uses the Name property of the instance.
        /// </summary>
        /// <param name="builder">The <see cref="RepositoryBuilder"/> instance.</param>
        /// <param name="policy">An <see cref="IOperationMethodPolicy"/> instance.</param>
        public static RepositoryBuilder UseOperationMethodExecutionPolicy(this RepositoryBuilder builder,
            IOperationMethodPolicy policy)
        {
            OperationCenter.Policies[policy.Name] = policy;
            return builder;
        }
        /// <summary>
        /// Adds an <see cref="IOperationMethodPolicy"/> implementation instance to the active policies.
        /// This method renames the policy (not uses the Name property of the instance).
        /// </summary>
        /// <param name="builder">The <see cref="RepositoryBuilder"/> instance.</param>
        /// <param name="name">New name of the policy.</param>
        /// <param name="policy">An <see cref="IOperationMethodPolicy"/> instance.</param>
        public static RepositoryBuilder UseOperationMethodExecutionPolicy(this RepositoryBuilder builder,
            string name, IOperationMethodPolicy policy)
        {
            OperationCenter.Policies[name] = policy;
            return builder;
        }
        /// <summary>
        /// Adds an inline Func&lt;IUser, OperationCallingContext, bool&gt; as OperationMethod execution policy.
        /// </summary>
        /// <param name="builder">The <see cref="RepositoryBuilder"/> instance.</param>
        /// <param name="name">Name of the policy.</param>
        /// <param name="policyFunction">The policy function to execute.</param>
        public static RepositoryBuilder UseOperationMethodExecutionPolicy(this RepositoryBuilder builder,
            string name, Func<IUser, OperationCallingContext, OperationMethodVisibility> policyFunction)
        {
            OperationCenter.Policies[name] = new InlineOperationMethodPolicy(name, policyFunction);
            return builder;
        }

        /// <summary>
        /// Writes the given text to the response body. UTF-8 encoding will be used.
        /// </summary>
        /// <param name="response">The <see cref="T:Microsoft.AspNetCore.Http.HttpResponse" />.</param>
        /// <param name="text">The text to write to the response.</param>
        /// <param name="cancellationToken">Notifies when request operations should be cancelled.</param>
        /// <returns>A task that represents the completion of the write operation.</returns>
        public static async Task WriteAsync(this HttpResponse response, string text,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Providers.Instance.GetProvider<IResponseLengthLimiter>()?.AssertLimit(response.HttpContext, text);
            await HttpResponseWritingExtensions.WriteAsync(response, text, cancellationToken);
        }

        /// <summary>
        /// Writes the given text to the response body using the given encoding.
        /// </summary>
        /// <param name="response">The <see cref="T:Microsoft.AspNetCore.Http.HttpResponse" />.</param>
        /// <param name="text">The text to write to the response.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="cancellationToken">Notifies when request operations should be cancelled.</param>
        /// <returns>A task that represents the completion of the write operation.</returns>
        public static async Task WriteAsync(this HttpResponse response, string text, Encoding encoding,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Providers.Instance.GetProvider<IResponseLengthLimiter>()?.AssertLimit(response.HttpContext, text);
            await HttpResponseWritingExtensions.WriteAsync(response, text, encoding, cancellationToken);
        }
    }
}
