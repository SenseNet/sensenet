using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.Services.Core.Authentication;

namespace SnWebApplication.Api.InMem.TokenAuth
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

            // [sensenet]: Authentication
            // Configure token authentication and add cookies so that non-script requests
            // (e.g. downloading files and images) work too.

            var authOptions = new AuthenticationOptions();
            Configuration.GetSection("sensenet:authentication").Bind(authOptions);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authOptions.Authority;
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;

                    options.Audience = "sensenet";

                    if (!string.IsNullOrWhiteSpace(authOptions.MetadataHost))
                        options.MetadataAddress =
                            $"{authOptions.MetadataHost.AddUrlSchema().TrimEnd('/')}/.well-known/openid-configuration";
                });

            // [sensenet]: add sensenet services
            services.AddSenseNet(Configuration, (repositoryBuilder, provider) =>
                {
                    repositoryBuilder
                        .BuildInMemoryRepository()
                        .UseLogger(provider)
                        .UseAccessProvider(new UserAccessProvider());
                })
                .AddSenseNetInMemoryProviders()
                .AddSenseNetOData()
                .AddSenseNetWebHooks()
                .AddSenseNetWopi()
                .AddSenseNetSemanticKernel(options =>
                {
                    Configuration.Bind("sensenet:ai:SemanticKernel", options);
                })
                .AddSenseNetAzureVision(options =>
                {
                    Configuration.Bind("sensenet:ai:AzureVision", options);
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                IdentityModelEventSource.ShowPII = true;
            }
            //app.UseHttpsRedirection();            

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
