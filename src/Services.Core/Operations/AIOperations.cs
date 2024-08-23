using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.AI.Text;
using SenseNet.AI.Vision;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using System;
using System.Threading.Tasks;

namespace SenseNet.Services.Core.Operations;

// ReSharper disable once InconsistentNaming
public static class AIOperations
{
    /// <summary>
    /// Generates the summary of a long text using AI.
    /// </summary>
    /// <snCategory>AI</snCategory>
    /// <param name="content"></param>
    /// <param name="maxWordCount">Maximum number of words in the summary.</param>
    /// <param name="maxSentenceCount">Maximum number of sentences in the summary.</param>
    /// <param name="context"></param>
    /// <param name="text">A long text to create summary from.</param>
    /// <returns>An object containing the summary.</returns>
    [ODataAction(Category = "AI")]
    [ContentTypes(N.CT.PortalRoot)]
    [AllowedRoles(N.R.AITextUsers)]
    public static async Task<object> GenerateSummary(Content content, HttpContext context,
        int maxWordCount, int maxSentenceCount, string text)
    {
        var summaryGenerator = context.RequestServices.GetService<ISummaryGenerator>() ??
                              throw new InvalidOperationException("AI summary generator is not available.");

        var summary = await summaryGenerator.GenerateSummaryAsync(text, maxWordCount, maxSentenceCount, 
            context.RequestAborted).ConfigureAwait(false);

        return new
        {
            summary
        };
    }

    /// <summary>
    /// Generates a content query from natural language text using AI.
    /// </summary>
    /// <snCategory>AI</snCategory>
    /// <param name="content"></param>
    /// <param name="context"></param>
    /// <param name="text">A natural language text to generate a from.</param>
    /// <param name="threadId">Optional thread id to chain requests.</param>
    /// <returns>An object containing the generated query and a thread ID.</returns>
    [ODataAction(Category = "AI")]
    [ContentTypes(N.CT.PortalRoot)]
    [AllowedRoles(N.R.AITextUsers)]
    public static async Task<object> GenerateContentQuery(Content content, HttpContext context, string text, string threadId = null)
    {
        //TODO: remove the root restriction and add context info to the query

        var queryGenerator = context.RequestServices.GetService<IContentQueryGenerator>() ??
                               throw new InvalidOperationException("AI content query generator is not available.");

        var queryData = await queryGenerator.GenerateQueryAsync(text, threadId, context.RequestAborted).ConfigureAwait(false);

        return new
        {
            queryData
        };
    }

    /// <summary>
    /// Generates an image from the provided text.
    /// </summary>
    /// <snCategory>AI</snCategory>
    /// <param name="content"></param>
    /// <param name="context"></param>
    /// <param name="text">The text to generate the image from.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <returns>The image data of the generated image.</returns>
    [ODataAction(Category = "AI")]
    [ContentTypes(N.CT.PortalRoot)]
    [AllowedRoles(N.R.AIVisionUsers)]
    public static async Task<object> GenerateImage(Content content, HttpContext context, string text, int width, int height)
    {
        //TODO: limit max parameters and text length by configuration
        //TODO: add caller permissions: allowed roles

        var imageGenerator = context.RequestServices.GetService<IImageGenerator>() ??
                              throw new InvalidOperationException("AI image generator is not available.");

        var imageData = await imageGenerator.GenerateImage(text, width, height, context.RequestAborted)
            .ConfigureAwait(false);

        return new
        {
            imageData
        };
    }
}