using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Packaging;
using SenseNet.Tools.SnAdmin.Testability;

namespace SnAdminRuntime.Tests.Implementations
{
    public class TestPackageManagerWrapper : IPackageManagerWrapper
    {
        public StringBuilder Log { get; } = new StringBuilder();

        public PackagingResult Execute(string packagePath, string targetDirectory, int phase, string[] parameters, TextWriter output, out Manifest manifest)
        {
            manifest = null;
            Log.AppendLine($"CALL: PackageManager({packagePath}, {targetDirectory}, {phase}, parameters:string[{parameters.Length}])");
            var result = new PackagingResult();
            var resultAcc = new PrivateObject(result);
            resultAcc.SetProperty("Successful", false);
            return result;
        }

        public string GetXmlSchema()
        {
            throw new NotImplementedException();
        }

        public string GetHelp()
        {
            throw new NotImplementedException();
        }
    }
}
