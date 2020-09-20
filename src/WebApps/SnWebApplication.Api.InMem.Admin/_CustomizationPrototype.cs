using System;
using Gyebi.TheCustomizer;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Packaging;
using SenseNet.Security;
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
    public class Feature1 : ISnComponent
    {
        public string ComponentId => typeof(Feature1).FullName;
        public Version SupportedVersion => null;
        public bool IsComponentAllowed(Version componentVersion)
        {
            return true;
        }

        public ISnPatch[] Patches => new ISnPatch[]
        {
            new ComponentInstaller
            {
                ComponentId = this.ComponentId,
                Version = new Version(1, 0),
                ReleaseDate = new DateTime(2020, 09, 18),
                Description = "My feature Feature1 description",
                Dependencies = new[]
                {
                    new Dependency
                    {
                        Id = "SenseNet.Services",
                        Boundary = new VersionBoundary {MinVersion = new Version(7, 7, 9)}
                    }
                },
                Execute = (context) =>
                {
                    using (new SystemAccount())
                    {
                        var folderName = "GyebiTesztel_v1.0";
                        var content = Content.Load($"/Root/{folderName}");
                        if (content == null)
                        {
                            var parent = Node.LoadNode("/Root");
                            content = Content.CreateNew("SystemFolder", parent, folderName);
                            content.Save();
                        }
                    }
                }
            },
            new SnPatch
            {
                ComponentId = this.ComponentId,
                Version = new Version(1, 1),
                ReleaseDate = new DateTime(2020, 09, 19),
                Description = "Feature1's PATCH to v1.1 description",
                Boundary = new VersionBoundary{MinVersion = new Version(1,0)},
                Dependencies = new[]
                {
                    new Dependency
                    {
                        Id = "SenseNet.Services",
                        Boundary = new VersionBoundary {MinVersion = new Version(7, 7, 9)}
                    }
                },
                Execute =  (context) =>
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
                }
            }
        };
    }
}
