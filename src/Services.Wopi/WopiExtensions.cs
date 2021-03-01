using Microsoft.AspNetCore.Builder;
using SenseNet.Services.Wopi;
using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class WopiExtensions
    {
        /// <summary>
        /// Registers the sensenet WOPI middleware in the pipeline
        /// if the request contains the wopi prefix.
        /// </summary>
        /// <param name="builder">IApplicationBuilder instance.</param>
        /// <param name="buildAppBranchBefore">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline before the sensenet WOPI middleware.</param>
        /// <param name="buildAppBranchAfter">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline after the sensenet WOPI middleware.</param>
        public static IApplicationBuilder UseSenseNetWopi(this IApplicationBuilder builder,
            Action<IApplicationBuilder> buildAppBranchBefore = null,
            Action<IApplicationBuilder> buildAppBranchAfter = null)
        {
            // add WOPI middleware if the request contains a prefix
            builder.MapMiddlewareWhen<WopiMiddleware>("/wopi", buildAppBranchBefore, 
                buildAppBranchAfter, true);

            // add the necessary execution policies
            builder.UseOperationMethodExecutionPolicy(new WopiOpenViewMethodPolicy())
                .UseOperationMethodExecutionPolicy(new WopiOpenEditMethodPolicy());

            return builder;
        }
    }
}
