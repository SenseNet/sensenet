using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyTitle("SnAdminRuntime (Debug)")]
#else
[assembly: AssemblyTitle("SnAdminRuntime (Release)")]
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
[assembly: AssemblyInformationalVersion("7.0.0-beta44")]

[assembly: ComVisible(false)]
[assembly: Guid("1B973251-9AAE-48D2-9FFF-408AA95CA576")]

[assembly: InternalsVisibleTo("SnAdminRuntime.Tests")]