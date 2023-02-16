using Microsoft.Extensions.Options;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.Packaging;

namespace SenseNet.ContentRepository.Components
{
    public class InMemoryClientStoreComponent : SnComponent
    {
        public override string ComponentId { get; } = "SenseNet.ClientStore.InMemory";

        public override void AddPatches(PatchBuilder builder)
        {
            builder.Install("1.0.0", "2023-02-16", "In memory implementation of Client store.")
                .DependsOn("SenseNet.Services", "7.7.23")
                .ActionOnBefore(context =>
                {
                    // no need to install anything
                })
                .Action(context =>
                {
                    // generate default clients and secrets
                    var clientStore = context.GetService<ClientStore>();
                    var clientOptions = context.GetService<IOptions<ClientStoreOptions>>().Value;

                    clientStore.EnsureClientsAsync(clientOptions.Authority,
                            clientOptions.RepositoryUrl.RemoveUrlSchema())
                        .GetAwaiter().GetResult();
                });
        }
    }
}
