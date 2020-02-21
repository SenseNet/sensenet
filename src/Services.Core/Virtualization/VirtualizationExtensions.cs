﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Services.Core.Virtualization
{
    public static class VirtualizationExtensions
    {
        /// <summary>
        /// Registers the sensenet binary middleware in the pipeline
        /// if the request contains the appropriate prefix or points to
        /// a file content directly.
        /// Add this middleware after authentication/authorization middlewares.
        /// </summary>
        /// <param name="builder">IApplicationBuilder instance.</param>
        /// <param name="buildAppBranch">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline after the sensenet binary middleware.</param>
        public static IApplicationBuilder UseSenseNetFiles(this IApplicationBuilder builder,
            Action<IApplicationBuilder> buildAppBranch = null)
        {
            // add binary middleware only if the request is recognized to be a binary request
            builder.MapWhen(IsBinaryRequest, appBranch =>
            {
                appBranch.UseMiddleware<BinaryMiddleware>();

                // Register a follow-up middleware defined by the caller or set a terminating, empty middleware.
                // If we do not do this, the system will try to set the status code which is not possible as
                // the request has already been started by our middleware above.

                if (buildAppBranch != null)
                    buildAppBranch.Invoke(appBranch);
                else
                    appBranch.Use((context, next) => Task.CompletedTask);
            });

            return builder;
        }

        private static bool IsBinaryRequest(HttpContext context)
        {
            if (context?.Request == null)
                return false;

            // if the request contains the binary handler prefix
            if (context.Request.Path.StartsWithSegments("/binaryhandler.ashx"))
                return true;

            //TODO: recognize urls containing the old 'sn-binary' prefix and
            // id, property and other parameters.
            
            // check if a content exists in the repository with this path
            if (NodeHead.Get(context.Request.Path) != null)
                return true;

            return false;
        }
    }
}
