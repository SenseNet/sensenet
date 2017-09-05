using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("SenseNet.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Services.Tests")]

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
[assembly: AssemblyVersion("7.0.0.0")]
[assembly: AssemblyFileVersion("7.0.0.0")]

// This attribute is used by NuGet to determine the package file name and version.
// It may contain a SemVer value.
[assembly: AssemblyInformationalVersion("7.0.0-beta44")]
