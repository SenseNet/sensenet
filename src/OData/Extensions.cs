using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SenseNet.OData.Writers;

namespace SenseNet.OData
{
    public static class Extensions
    {
        public static IApplicationBuilder UseSenseNetOdata(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ODataMiddleware>();
        }
    }
}
