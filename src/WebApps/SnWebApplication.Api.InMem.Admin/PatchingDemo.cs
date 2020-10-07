//UNDONE: Delete this file and the line in the Setup.cs,

using System;

namespace SenseNet.Extensions.DependencyInjection
{
    using Gyebi.TheCustomizer;
    using Microsoft.Extensions.DependencyInjection;

    public static class Extensions
    {
        public static IServiceCollection AddFeature1(this IServiceCollection services)
        {
            // add services required by this feature
            services.AddComponent(provider => new Feature1());

            return services;
        }
    }
}

namespace Gyebi.TheCustomizer
{
    using SenseNet.ContentRepository;
    using SenseNet.ContentRepository.Storage;
    using SenseNet.ContentRepository.Storage.Security;
    using SenseNet.Packaging;

    public class Feature1 : SnComponent
    {
        public override string ComponentId => typeof(Feature1).FullName;

        public override void AddPatches(PatchBuilder builder)
        {
            var dependencies = new DependencyBuilder()
                .Dependency("SenseNet.Services", "7.7.9");

            builder.Patch("1.0", "1.1", "2020-02-10", "Feature1's PATCH to v1.1 description")
                .DependsOn(dependencies)
                .Action(context =>
                {
                    var oldFolderName = "GyebiTesztel_v1.0";
                    var newFolderName = "GyebiTesztel_v1.1";
                    var newContent = Content.Load($"/Root/{newFolderName}");
                    if (newContent != null)
                        return;
                    var oldContent = Content.Load($"/Root/{oldFolderName}");
                    if (oldContent != null)
                    {
                        oldContent.ContentHandler.Name = newFolderName;
                        oldContent.Save();
                    }
                    else
                    {
                        var parent = Node.LoadNode("/Root");
                        newContent = Content.CreateNew("SystemFolder", parent, newFolderName);
                        newContent.Save();
                    }
                });

            builder.Install("1.1", "2020-02-10", "My feature Feature1 description")
                .DependsOn(dependencies)
                .Action((context) =>
                {
                    var folderName = "GyebiTesztel_v1.1";
                    var content = Content.Load($"/Root/{folderName}");
                    if (content == null)
                    {
                        var parent = Node.LoadNode("/Root");
                        content = Content.CreateNew("SystemFolder", parent, folderName);
                        content.Save();
                    }
                });
        }
    }
}
