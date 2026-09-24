using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.ApiKeys;
using SenseNet.Diagnostics;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search.Lucene29;
using SenseNet.Services.Core.Authentication;

namespace SnWebApplication.Api.Sql.TokenAuth.Preview
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            // [sensenet]: Authentication
            var authOptions = new AuthenticationOptions();
            Configuration.GetSection("sensenet:authentication").Bind(authOptions);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;

                    if (authOptions.AuthServerType == AuthenticationServerType.SNAuth)
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = false
                        };

                        var snAuthUrl = !string.IsNullOrEmpty(authOptions.MetadataHost)
                            ? authOptions.MetadataHost
                            : authOptions.Authority;

                        options.TokenHandlers.Clear();
                        options.TokenHandlers.Add(new SenseNetJwtSecurityTokenHandler(
                            $"{snAuthUrl}/api/auth/validate-token"));
                    }
                    else
                    {
                        options.Audience = "sensenet";

                        options.Authority = authOptions.Authority;
                        if (!string.IsNullOrWhiteSpace(authOptions.MetadataHost))
                            options.MetadataAddress =
                        $"{authOptions.MetadataHost.AddUrlSchema().TrimEnd('/')}/.well-known/openid-configuration";
                    }
                });

            // [sensenet]: Set options for ApiKeys
            services.Configure<ApiKeysOptions>(Configuration.GetSection("sensenet:ApiKeys"));

            // [sensenet]: Set options for EFCSecurityDataProvider
            services.AddOptions<SenseNet.Security.EFCSecurityStore.Configuration.DataOptions>()
                .Configure<IOptions<ConnectionStringOptions>>((securityOptions, systemConnections) =>
                    securityOptions.ConnectionString = systemConnections.Value.Security);

            // [sensenet]: add sensenet services
            services
                .AddSenseNetInstallPackage()
                .AddSenseNet(Configuration, (repositoryBuilder, provider) =>
                {
                    var searchEngineLogger = repositoryBuilder.Services.GetService<ILogger<Lucene29SearchEngine>>();
                    repositoryBuilder
                        .UseLogger(provider)
                        .UseLucene29LocalSearchEngine(searchEngineLogger, 
                            Path.Combine(Environment.CurrentDirectory, "App_Data", "LocalIndex"));
                })
                .AddEFCSecurityDataProvider()
                .AddSenseNetMsSqlProviders(configureInstallation: installOptions =>
                {
                    Configuration.Bind("sensenet:install:mssql", installOptions);
                })
                .AddSenseNetOData()
                .AddSenseNetWebHooks()
                .AddSenseNetWopi()
                .AddAsposeDocumentPreviewProvider(options =>
                {
                    options.SkipLicenseCheck =
                        Configuration.GetValue("sensenet:AsposePreviewProvider:SkipLicenseCheck", false);
                })
                .AddSenseNetSemanticKernel(options =>
                {
                    Configuration.Bind("sensenet:ai:SemanticKernel", options);
                })
                .AddSenseNetAzureVision(options =>
                {
                    Configuration.Bind("sensenet:ai:AzureVision", options);
                });

            // [sensenet]: statistics overrides
            var statOptions = new StatisticsOptions();
            Configuration.GetSection("sensenet:statistics").Bind(statOptions);
            if (!statOptions.Enabled)
            {
                // reset to default/null services
                services
                    .AddDefaultStatisticalDataProvider()
                    .AddDefaultStatisticalDataCollector();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // [sensenet]: custom CORS policy
            app.UseSenseNetCors();
            // [sensenet]: use Authentication and set User.Current
            app.UseSenseNetAuthentication();

            // [sensenet]: MembershipExtender middleware
            app.UseSenseNetMembershipExtenders();

            app.UseAuthorization();

            // [sensenet] Add the sensenet binary handler
            app.UseSenseNetFiles();

            // [sensenet]: Health middleware
            app.UseSenseNetHealth();
            // [sensenet]: OData middleware
            app.UseSenseNetOdata();
            // [sensenet]: WOPI middleware
            app.UseSenseNetWopi();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("sensenet is listening. Visit https://sensenet.com for " +
                                                      "more information on how to call the REST API.");
                });
            });
        }
    }
}
