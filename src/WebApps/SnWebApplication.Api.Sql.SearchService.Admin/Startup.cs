using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Search.Lucene29.Centralized;
using SenseNet.Search.Lucene29.Centralized.GrpcClient;
using SenseNet.Security.Messaging.RabbitMQ;

namespace SnWebApplication.Api.Sql.SearchService.Admin
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

            // [sensenet]: Set options for EFCSecurityDataProvider
            services.AddOptions<SenseNet.Security.EFCSecurityStore.Configuration.DataOptions>()
                .Configure<IOptions<ConnectionStringOptions>>((securityOptions, systemConnections) =>
                    securityOptions.ConnectionString = systemConnections.Value.Security);

            // [sensenet]: add sensenet services
            services
                .AddSenseNetInstallPackage()
                .AddSenseNet(Configuration, (repositoryBuilder, provider) =>
                {
                    repositoryBuilder
                        .UseLogger(provider)
                        //UNDONE: MSSQLSEPARATE move this call to the common SQL registration method (make sure it is registered!)
                        .UseMsSqlExclusiveLockDataProvider();
                })
                .AddEFCSecurityDataProvider()
                .AddSenseNetMsSqlProviders(installOptions =>
                {
                    Configuration.Bind("sensenet:install:mssql", installOptions);
                })
                .Configure<GrpcClientOptions>(Configuration.GetSection("sensenet:search:service"))
                .Configure<CentralizedOptions>(Configuration.GetSection("sensenet:search:service"))
                .Configure<RabbitMqOptions>(Configuration.GetSection("sensenet:security:rabbitmq"))
                .AddLucene29CentralizedSearchEngineWithGrpc()
                .AddRabbitMqSecurityMessageProvider()
                .AddRabbitMqMessageProvider(configureRabbitMq: options =>
                {
                    Configuration.GetSection("sensenet:rabbitmq").Bind(options);
                })
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

            // [sensenet]: Authentication: in this test project everybody
            // is an administrator!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            app.Use(async (context, next) =>
            {
                User.Current = User.Administrator;
                if (next != null)
                    await next();
            });

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
