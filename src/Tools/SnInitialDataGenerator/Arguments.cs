using System;
using SenseNet.Tools.CommandLineArguments;

namespace SenseNet.Tools.SnInitialDataGenerator
{
    internal class Arguments
    {
        public const string DefaultDatabaseName = "<__namespace__>.__InitialData";
        public const string DefaultIndexName = "<__namespace__>.__InitialIndex";
        public const string DefaultDataFileName = "__InitialData.cs";
        public const string DefaultIndexFileName = "__InitialIndex.cs";

        [CommandLineArgument(Name = "Import", Required = true, Aliases = new[] { "I" }, HelpText = "Filesystem path of the import material.")]
        public string ImportPath { get; set; }

        [CommandLineArgument(Name = "Output", Required = false, Aliases = new [] {"O"}, HelpText = "Filesystem path of the output directory. Default: <App>/output")]
        public string OutputPath { get; set; }

        [CommandLineArgument(Name = "TypeName", Required = false, Aliases = new[] { "T", "Type" }, HelpText = "Fully qualified name of the generated C# type. Default: '" + DefaultDatabaseName + "'")]
        public string TypeName { get; set; }
        public string Namespace { get; set; }
        public string ClassName { get; set; }

        [CommandLineArgument(Name = "DataFile", Required = false, Aliases = new[] { "D", "Data" }, HelpText = "Filesystem path of the generated C# file. Default: " + DefaultDataFileName + ". The file name can be absolute or relative to the '<App>/output' directory. Warning: the generated file overrides the existing one.")]
        public string DataFileName { get; set; }

        

        [CommandLineArgument(Name = "IndexFile", Required = false, Aliases = new[] { "Index" },
            HelpText = "Filesystem path of the generated C# file. Default: " + DefaultIndexFileName + ". " +
                       "The file name can be absolute or relative to the '<App>/output' directory. " +
                       "Warning: the generated file overrides the existing one.")]
        public string IndexFileName { get; set; }
    }
}
