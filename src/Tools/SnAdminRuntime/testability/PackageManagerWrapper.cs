using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Packaging;

namespace SenseNet.Tools.SnAdmin.Testability
{
    internal interface IPackageManagerWrapper
    {
        PackagingResult Execute(string packagePath, string targetDirectory, int phase, string[] parameters, TextWriter output);
        string GetXmlSchema();
        string GetHelp();
    }

    internal static class PackageManagerWrapper
    {
        public static IPackageManagerWrapper Instance { get; set; } = new BuiltInPackageManagerWrapper();
    }

    internal class BuiltInPackageManagerWrapper : IPackageManagerWrapper
    {
        public PackagingResult Execute(string packagePath, string targetDirectory, int phase, string[] parameters, TextWriter output)
        {
            return PackageManager.Execute(packagePath, targetDirectory, phase, parameters, output, null, true);
        }

        public string GetXmlSchema()
        {
            return PackageManager.GetXmlSchema();
        }

        public string GetHelp()
        {
            return PackageManager.GetHelp();
        }
    }
}
