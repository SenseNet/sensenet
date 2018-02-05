using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("SenseNet.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Services.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

#if DEBUG
[assembly: AssemblyTitle("SenseNet.Services (Debug)")]
#else
[assembly: AssemblyTitle("SenseNet.Services (Release)")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Sense/Net Inc.")]
[assembly: AssemblyCopyright("Copyright © Sense/Net Inc.")]
[assembly: AssemblyProduct("sensenet ECM")]
[assembly: AssemblyTrademark("Sense/Net Inc.")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("7.1.0.0")]
[assembly: AssemblyFileVersion("7.1.0.0")]

// This attribute is used by NuGet to determine the package file name and version.
// It may contain a SemVer value.
[assembly: AssemblyInformationalVersion("7.1.0")]
