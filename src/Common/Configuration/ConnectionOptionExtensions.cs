using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection;

public static class ConnectionOptionExtensions
{
    public static IServiceCollection ConfigureConnectionStrings(this IServiceCollection services, IConfiguration configuration)
    {
        var defaultConnectionString = configuration.GetConnectionString("SnCrMsSql");
        return services.Configure<ConnectionStringOptions>(options =>
        {
            options.Repository = defaultConnectionString;
            options.Security = configuration.GetConnectionString("SecurityStorage") ?? defaultConnectionString;
            options.SignalR = configuration.GetConnectionString("SignalRDatabase") ?? defaultConnectionString;

            var section = configuration.GetSection("ConnectionStrings");
            options.AllConnectionStrings = section.GetChildren()
                .ToDictionary(x => x.Key, x => x.Value);
        });
    }

}