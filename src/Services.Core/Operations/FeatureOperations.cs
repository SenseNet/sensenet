using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.Tools.Features;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Operations;

// ReSharper disable once InconsistentNaming
public static class FeatureOperations
{
    #region Helper classes
    
    /// <summary>
    /// View object for feature information.
    /// </summary>
    internal class FeatureInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public FeatureAvailabilityInfo State { get; set; }
    }

    /// <summary>
    /// View object for feature availability.
    /// </summary>
    internal class FeatureAvailabilityInfo
    {
        /// <summary>
        ///  State of the feature.
        /// </summary>
        public string State { get; private set; }

        /// <summary>
        /// Reason if the feature is not available.
        /// </summary>
        public string Reason { get; private set; }

        /// <summary>
        /// Last time the feature was available.
        /// </summary>
        public DateTime? LastAvailable { get; private set; }

        internal static FeatureAvailabilityInfo FromFeatureAvailability(FeatureAvailability featureAvailability)
        {
            return new FeatureAvailabilityInfo
            {
                State = featureAvailability.State.ToString(),
                Reason = featureAvailability.Reason,
                LastAvailable = featureAvailability.LastAvailable
            };
        }
    }

    #endregion

    // private in-memory cache for the feature list
    private static readonly IMemoryCache FeatureCache = new MemoryCache(new MemoryCacheOptions());

    /// <summary>
    /// Gets the list of registered features.
    /// </summary>
    /// <returns></returns>
    [ODataFunction]
    [ContentTypes(N.CT.PortalRoot)]
    [AllowedRoles(N.R.Everyone)]  
    public static async Task<object> GetFeatures(Content content, HttpContext context)
    {
        const string cacheKey = "features";

        if (FeatureCache.TryGetValue(cacheKey, out var cachedFeatures))
            return cachedFeatures;

        var logger = context.RequestServices.GetRequiredService<ILogger<FeatureInfo>>();
        var snFeatures = context.RequestServices.GetServices<ISnFeature>();

        logger.LogTrace("Collecting available features.");

        var features = await Task.WhenAll(snFeatures.Select(async snFeature =>
        {
            var state = await snFeature.GetStateAsync(context.RequestAborted).ConfigureAwait(false);

            var featureInfo = new FeatureInfo
            {
                Name = snFeature.Name,
                DisplayName = snFeature.DisplayName,
                State = FeatureAvailabilityInfo.FromFeatureAvailability(state)
            };

            //TODO: permission check for certain features

            return featureInfo;
        }));

        logger.LogTrace("Adding feature list with {count} items to the cache.", features.Length);

        FeatureCache.Set(cacheKey, features, TimeSpan.FromSeconds(30));

        return new
        {
            features
        };
    }
}