using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Components;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search.Lucene29.Centralized;
using SenseNet.Search.Lucene29.Centralized.GrpcClient;
using SenseNet.Security.Messaging.RabbitMQ;

namespace SnWebApplication.Api.Sql.SearchService.TokenAuth
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
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = Configuration["sensenet:authentication:authority"];
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;

                    options.Audience = "sensenet";
                });

            // [sensenet]: add sensenet services
            services
                .AddSenseNetInstallPackage()
                .AddSenseNet(Configuration, (repositoryBuilder, provider) =>
            {
                repositoryBuilder
                    .UseLogger(provider)
                    .UseMsSqlExclusiveLockDataProviderExtension();
            })
                .AddEFCSecurityDataProvider(options =>
                {
                    options.ConnectionString = ConnectionStrings.ConnectionString;
                })
                .Configure<GrpcClientOptions>(Configuration.GetSection("sensenet:search:service"))
                .Configure<CentralizedOptions>(Configuration.GetSection("sensenet:search:service"))
                .Configure<RabbitMqOptions>(Configuration.GetSection("sensenet:security:rabbitmq"))
                .AddLucene29CentralizedSearchEngineWithGrpc()
                .AddRabbitMqSecurityMessageProvider()
                .AddSenseNetMsSqlStatisticalDataProvider()
                .AddSenseNetMsSqlClientStoreDataProvider()
                .AddComponent(provider => new MsSqlExclusiveLockComponent())
                .AddComponent(provider => new MsSqlStatisticsComponent())
                .AddComponent(provider => new MsSqlClientStoreComponent())
                .AddSenseNetWebHooks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

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
