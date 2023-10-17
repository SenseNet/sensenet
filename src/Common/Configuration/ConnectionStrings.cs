using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SenseNet.Tools.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    /// <summary>
    /// Contains connection strings of the repository and all used component.
    /// </summary>
    [OptionsClass(sectionName: "ConnectionStrings")]
    public class ConnectionStringOptions
    {
        [JsonProperty("SnCrMsSql")]
        public string Repository { get; set; }

        private string _security;
        [JsonProperty("SecurityStorage")]
        public string Security
        {
            get => _security ?? Repository;
            set => _security = value;
        }

        private string _signalR;
        [JsonProperty("SignalRDatabase")]
        public string SignalR
        {
            get => _signalR ?? Repository;
            set => _signalR = value;
        }

        public IDictionary<string, string> AllConnectionStrings { get; set; }
    }

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
}
