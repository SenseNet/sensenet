﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Virtualization;
using SenseNet.Services.Core.Virtualization;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
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
        /// <param name="buildAppBranchBefore">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline before the sensenet binary middleware.</param>
        ///  <param name="buildAppBranchAfter">Optional builder method. Use this when you want to add
        /// additional middleware in the pipeline after the sensenet binary middleware.</param>
        public static IApplicationBuilder UseSenseNetFiles(this IApplicationBuilder builder,
            Action<IApplicationBuilder> buildAppBranchBefore = null,
            Action<IApplicationBuilder> buildAppBranchAfter = null)
        {
            // add binary middleware only if the request is recognized to be a binary request
            builder.MapMiddlewareWhen<BinaryMiddleware>(IsBinaryRequest, buildAppBranchBefore,
                buildAppBranchAfter, true);

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
            
            //TODO: check case sensitivity
            // try to load it as a content
            var head = context.Request.GetNodeHead();

            // we are able to handle file types
            return head != null && head.GetNodeType().IsInstaceOfOrDerivedFrom("File");
        }

        internal static NodeHead GetNodeHead(this HttpRequest request)
        {
            // This is necessary so we can recognize special characters like space or UTF characters 
            // that were encoded by the client.
            var decoded = WebUtility.UrlDecode(request.Path);
            return NodeHead.Get(decoded);
        }
    }
}
