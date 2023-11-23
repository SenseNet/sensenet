using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.AI.Abstractions;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using System;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Operations
{
    // ReSharper disable once InconsistentNaming
    public static class AIOperations
    {
        /// <summary>
        /// Gets the summary of a long text using AI.
        /// </summary>
        /// <snCategory>AI</snCategory>
        /// <param name="content"></param>
        /// <param name="context"></param>
        /// <param name="text">A long text to create summary from.</param>
        /// <returns>An object containing the summary.</returns>
        [ODataAction]
        [ContentTypes(N.CT.PortalRoot)]
        [AllowedRoles(N.R.Administrators)]
        public static async Task<object> GetSummary(Content content, HttpContext context, string text)
        {
            //TODO: add max length parameters
            //TODO: maybe check text length?

            var textService = context.RequestServices.GetService<ITextService>() ??
                              throw new InvalidOperationException("AI TextService is not available.");

            var summary = await textService.GetSummary(text, context.RequestAborted).ConfigureAwait(false);

            return new
            {
                summary
            };
        }
    }
}
