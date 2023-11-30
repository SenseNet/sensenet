using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.Tools.Features;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.Core.Operations;

// ReSharper disable once InconsistentNaming
public static class FeatureOperations
{
    internal class FeatureInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public FeatureAvailability State { get; set; }
    }

    /// <summary>
    /// Gets the list of registered features.
    /// </summary>
    /// <returns></returns>
    [ODataFunction]
    [ContentTypes(N.CT.PortalRoot)]
    [AllowedRoles(N.R.Everyone)]  
    public static async Task<object> GetFeatures(Content content, HttpContext context)
    {
        //TODO: cache the result for a while

        var snFeatures = context.RequestServices.GetServices<ISnFeature>();

        var features = await Task.WhenAll(snFeatures.Select(async snFeature =>
        {
            var state = await snFeature.GetStateAsync(context.RequestAborted).ConfigureAwait(false);

            var featureInfo = new FeatureInfo
            {
                Name = snFeature.Name,
                DisplayName = snFeature.DisplayName,
                State = state
            };

            //TODO: permission check for certain features

            return featureInfo;
        }));

        return new
        {
            features
        };
    }
}