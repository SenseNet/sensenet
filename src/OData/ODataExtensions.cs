using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.OData;
using SenseNet.OData.Metadata;
using SenseNet.OData.Operations;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents an OData controller in the DI container.
    /// </summary>
    public class ODataControllerRegistration
    {
        /// <summary>
        /// Gets the name of the controller.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets the type of the controller.
        /// </summary>
        public Type Type { get; set; }
    }

    public static class ODataExtensions
    {
        /// <summary>
        /// Adds the required services for the sensenet OData layer.
        /// </summary>
        public static IServiceCollection AddSenseNetOData(this IServiceCollection services)
        {
            return services
                .AddSingleton<OperationInspector>()
                .AddSingleton<IOperationMethodStorage, OperationMethodStorage>()
                .AddSingleton<IClientMetadataProvider, ClientMetadataProvider>()
                .AddSingleton<IODataControllerFactory, ODataControllerFactory>()
                .AddSenseNetODataController<HelpController>("Help")
                ;
        }

        /// <summary>
        /// Registers a custom OData controller in the DI container.
        /// </summary>
        public static IServiceCollection AddSenseNetODataController<TImpl>(this IServiceCollection services, string name = null)
            where TImpl : ODataController
        {
            if (name != null && name.Length == 0)
                name = null;

            services.AddTransient<TImpl>();
            services.AddSingleton(new ODataControllerRegistration
            {
                Name = name?.Trim() ?? typeof(TImpl).Name,
                Type = typeof(TImpl)
            });

            return services;
        }

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
