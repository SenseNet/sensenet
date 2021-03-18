using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SenseNet.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Tests.Core")]
[assembly: InternalsVisibleTo("SenseNet.BlobStorage.IntegrationTests")] //UNDONE:<?: Delete if SenseNet.BlobStorage.IntegrationTests is inactivated
[assembly: InternalsVisibleTo("SenseNet.IntegrationTests")]
[assembly: InternalsVisibleTo("SenseNet.IntegrationTests.Common")]
[assembly: InternalsVisibleTo("SenseNet.ContentRepository.Tests")]
[assembly: InternalsVisibleTo("SenseNet.ContentRepository.IntegrationTests")]
[assembly: InternalsVisibleTo("SenseNet.Storage.IntegrationTests")]
[assembly: InternalsVisibleTo("SenseNet.Packaging.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Packaging.IntegrationTests")]

[assembly: AssemblyTrademark("Sense/Net Inc.")]
