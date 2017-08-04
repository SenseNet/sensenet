using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("SenseNet.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Packaging.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Packaging.IntegrationTests")]
[assembly: InternalsVisibleTo("SenseNet.SearchImpl.Tests")]

#if DEBUG
[assembly: AssemblyTitle("SenseNet.ContentRepository (Debug)")]
#else
[assembly: AssemblyTitle("SenseNet.ContentRepository (Release)")]
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
[assembly: AssemblyInformationalVersion("7.0.0-beta3")]
