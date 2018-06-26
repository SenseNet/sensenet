using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("SenseNet.Tests")]
[assembly: InternalsVisibleTo("SenseNet.ContentRepository.Tests")]
[assembly: InternalsVisibleTo("SenseNet.Search.Lucene29.Tests")]

#if DEBUG
[assembly: AssemblyTitle("SenseNet.BlobStorage (Debug)")]
#else
[assembly: AssemblyTitle("SenseNet.BlobStorage (Release)")]
#endif

[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Sense/Net Inc.")]
[assembly: AssemblyCopyright("Copyright © Sense/Net Inc.")]
[assembly: AssemblyProduct("sensenet")]
[assembly: AssemblyTrademark("Sense/Net Inc.")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("7.2.0.0")]
[assembly: AssemblyFileVersion("7.2.0.0")]
[assembly: AssemblyInformationalVersion("7.2.0")]
