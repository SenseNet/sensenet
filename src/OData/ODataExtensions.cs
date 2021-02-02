using System;
using Microsoft.AspNetCore.Builder;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class ODataExtensions
    {
        /// <summary>
        /// Registers the sensenet OData middleware in the pipeline
        /// if the request contains the odata.svc prefix.
        /// </summary>
        /// <param name="builder">IApplicationBuilder instance.</param>
        /// <param name="buildAppBranchBefore">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline before the sensenet OData middleware.</param>
        /// <param name="buildAppBranchAfter">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline after the sensenet OData middleware.</param>
        public static IApplicationBuilder UseSenseNetOdata(this IApplicationBuilder builder, 
            Action<IApplicationBuilder> buildAppBranchBefore = null,
            Action<IApplicationBuilder> buildAppBranchAfter = null)
        {
            // add OData middleware only if the request contains the appropriate prefix
            builder.MapMiddlewareWhen<ODataMiddleware>("/odata.svc", buildAppBranchBefore, 
                buildAppBranchAfter, true);

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
    }
}
