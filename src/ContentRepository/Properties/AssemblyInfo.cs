using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("SenseNet.Tests")]
[assembly: InternalsVisibleTo("SenseNet.BlobStorage.IntegrationTests")]
[assembly: InternalsVisibleTo("SenseNet.ContentRepository.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Packaging.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Packaging.IntegrationTests")]
[assembly: InternalsVisibleTo("SenseNet.Search.Lucene29.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Search.IntegrationTests")]
[assembly: InternalsVisibleTo("SenseNet.Services.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Services.OData.Tests")]

#if DEBUG
[assembly: AssemblyTitle("SenseNet.ContentRepository (Debug)")]
#else
[assembly: AssemblyTitle("SenseNet.ContentRepository (Release)")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Sense/Net Inc.")]
[assembly: AssemblyCopyright("Copyright © Sense/Net Inc.")]
[assembly: AssemblyProduct("sensenet")]
[assembly: AssemblyTrademark("Sense/Net Inc.")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("7.5.0.3")]
[assembly: AssemblyFileVersion("7.5.0.3")]
[assembly: AssemblyInformationalVersion("7.5.0.3")]
