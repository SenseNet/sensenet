using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("SenseNet.Tests")]
[assembly: InternalsVisibleTo("SenseNet.ContentRepository.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Services.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("SenseNet.Services.OData.Tests")]

#if DEBUG
[assembly: AssemblyTitle("SenseNet.Services (Debug)")]
#else
[assembly: AssemblyTitle("SenseNet.Services (Release)")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Sense/Net Inc.")]
[assembly: AssemblyCopyright("Copyright © Sense/Net Inc.")]
[assembly: AssemblyProduct("sensenet")]
[assembly: AssemblyTrademark("Sense/Net Inc.")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("7.7.4")]
[assembly: AssemblyFileVersion("7.7.4")]
[assembly: AssemblyInformationalVersion("7.7.4")]
