using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Extensions.DependencyInjection;
using SenseNet.WebHooks;
using Task = System.Threading.Tasks.Task;

namespace SnWebApplication.Api.InMem.Admin
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

            // [sensenet]: Authentication: switched off below
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

            // [sensenet]: add sensenet services
            services.AddSenseNet(Configuration, (repositoryBuilder, provider) =>
                {
                    repositoryBuilder
                        .BuildInMemoryRepository()
                        .UseLogger(provider)
                        .UseAccessProvider(new UserAccessProvider())
                        .UseInactiveAuditEventWriter();
                }, (instance, provider) =>
                {
                    using (new SystemAccount())
                    {
                        //UNDONE: remove sample webhook subscription code
                        var webhooks = Node.LoadNode("/Root/System/WebHooks");

                        var wh1 = new WebHookSubscription(webhooks)
                        {
                            Name = "wh1",
                            Url = "https://localhost:44393/webhooks/test",
                            Filter = "{ \"Path\": \"/Root/Content\", \"ContentTypes\": [ { \"Name\": \"Folder\", \"Events\": [ \"Create\" ] } ] }",
                            Enabled = true
                        };
                        wh1.Save();

                        var wh2 = new WebHookSubscription(webhooks)
                        {
                            Name = "wh2",
                            Url = "https://localhost:44393/webhooks/test",
                            Filter = "{ \"Path\": \"/Root/Content\", \"ContentTypes\": [ { \"Name\": \"File\", \"Events\": [ \"All\" ] } ] }",
                            Headers = "{ \"h1-custom\": \"value1\", \"h2-custom\": \"value2\" }",
                            Enabled = true
                        };
                        wh2.Save();
                    }
                    return Task.CompletedTask;
                })
                .AddSenseNetWebHooks()
                .AddFeature1();
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
