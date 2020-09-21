﻿using System;
using Gyebi.TheCustomizer;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Packaging;
using SenseNet.Tools;

namespace SenseNet.Extensions.DependencyInjection
{
    public static class Extensions
    {
        public static IRepositoryBuilder UseFeature1(this IRepositoryBuilder repositoryBuilder, Feature1 instance)
        {
            Configuration.Providers.Instance.SetProvider(typeof(Feature1), instance);
            Configuration.Providers.Instance.AddComponent(instance);
            return repositoryBuilder;
        }
    }
}

namespace Gyebi.TheCustomizer
{
    public class Feature1 : SnComponent
    {
        public override string ComponentId => typeof(Feature1).FullName;
        public override Version SupportedVersion => null;

        public override bool IsComponentAllowed(Version componentVersion)
        {
            return true;
        }

        public override void AddPatches(PatchBuilder builder)
        {
            DependencyBuilder Dependencies()
            {
                return builder
                    //.Dependency("SenseNet.Services", builder.Version("7.7.9.2"))
                    //.Dependency("SenseNet.Services", "7.7.9")
                    .Dependency("SenseNet.Services", builder.MinVersion("7.7.9"))
                    //.Dependency("SenseNet.Services", builder.MinMaxExVersion("7.7.9", "7.7.10"))
                    ;
            }

            builder.Patch("1.1", "2020-02-10", "My feature Feature1 description",
                    builder.MinVersion("1.0"),
                    Dependencies(),
                    (context) =>
                    {
                        using (new SystemAccount())
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
                        }
                    })
                .Install("1.1", "2020-02-10", "Feature1's PATCH to v1.1 description",
                    Dependencies(),
                    (context) =>
                    {
                        using (new SystemAccount())
                        {
                            var folderName = "GyebiTesztel_v1.1";
                            var content = Content.Load($"/Root/{folderName}");
                            if (content == null)
                            {
                                var parent = Node.LoadNode("/Root");
                                content = Content.CreateNew("SystemFolder", parent, folderName);
                                content.Save();
                            }
                        }
                    });
        }
    }
}
