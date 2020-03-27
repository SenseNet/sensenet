using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Tools.CommandLineArguments;

namespace SenseNet.Tools.SnInitialDataGenerator
{
    internal class Arguments
    {
        public const string DefaultNamespace = "__namespace__";
        public const string DefaultDatabaseName = "__InitialData";
        public const string DefaultIndexName = "__InitialIndex";
        public const string DefaultDataFileName = "__InitialData.cs";
        public const string DefaultIndexFileName = "__InitialIndex.cs";

        [CommandLineArgument(Name = "Import", Required = true, Aliases = new[] { "I" }, 
            HelpText = "Filesystem path of the import material.")]
        public string ImportPath { get; set; }

        [CommandLineArgument(Name = "Skip", Required = false, Aliases = new[] { "S" },
            HelpText = "Comma separated path list that will be skipped during import. " +
                       "Every path needs to be relative to <ImportPath> and under to it.")]
        public string SkippedPaths { get; set; }

        [CommandLineArgument(Name = "SkipFile", Required = false, Aliases = new[] { "SF" },
            HelpText = "Path of an existing file that contains a path list will be skipped during import. " +
                       "Each line in the file is a skipped path " +
                       "Every path needs to be relative to <ImportPath> and under to it.")]
        public string SkipFile { get; set; }
        public string[] SkippedPathArray { get; private set; }

        [CommandLineArgument(Name = "Output", Required = false, Aliases = new [] {"O"},
            HelpText = "Filesystem path of the output directory. Default: <App>/output")]
        public string OutputPath { get; set; }

        [CommandLineArgument(Name = "DataFile", Required = false, Aliases = new[] { "DF" },
            HelpText = "Filesystem path of the generated C# file. Default: " + DefaultDataFileName + ". " +
                       "The file name can be absolute or relative to the '<App>/output' directory. " +
                       "Warning: the generated file overrides the existing one.")]
        public string DataFileName { get; set; }
        
        [CommandLineArgument(Name = "IndexFile", Required = false, Aliases = new[] { "IF" },
            HelpText = "Filesystem path of the generated C# file. Default: " + DefaultIndexFileName + ". " +
                       "The file name can be absolute or relative to the '<App>/output' directory. " +
                       "Warning: the generated file overrides the existing one.")]
        public string IndexFileName { get; set; }

        [CommandLineArgument(Required = false, Aliases = new[] { "DT" },
            HelpText = "Name or fully qualified name of the generated C# database type. " +
                       "Default: '" + DefaultNamespace +"." + DefaultDatabaseName + "'")]
        public string DatabaseTypeName { get; set; }
        public string DatabaseNamespace { get; set; }
        public string DatabaseClassName { get; set; }

        [CommandLineArgument(Required = false, Aliases = new[] { "IT" },
            HelpText = "Name or fully qualified name of the generated C# index type. " +
                       "Default: '" + DefaultNamespace + "." + DefaultIndexName + "'")]
        public string IndexTypeName { get; set; }
        public string IndexNamespace { get; set; }
        public string IndexClassName { get; set; }

        public void Prepare()
        {
            PrepareProperties();
            PrepareFileSystemEntries();
        }
        internal void PrepareProperties()
        {
            // IMPORT PATH
            ImportPath = Path.GetFullPath(ImportPath);

            // SKIPPED PATHS
            // Skip file will be loaded in the PrepareFileSystemEntries.
            var paths = string.IsNullOrEmpty(SkippedPaths) ? new string[0] : SkippedPaths.Split(',');
            for (var i = 0; i < paths.Length; i++)
            {
                if (!Path.IsPathRooted(paths[i]))
                    paths[i] = Path.GetFullPath(Path.Combine(ImportPath, paths[i]));
                if (!paths[i].StartsWith(ImportPath, StringComparison.CurrentCultureIgnoreCase))
                    throw new ArgumentException("One or more paths are out of the ImportPath");
                paths[i] = paths[i].Substring(ImportPath.Length).Replace("\\", "/");
                paths[i] = paths[i].Length == 0 || paths[i] == "/"
                    ? "/Root"
                    : "/Root" + paths[i];
            }
            SkippedPathArray = paths;

            // OUTPUT DIRECTORY
            if (OutputPath == null)
                OutputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");

            // DATA FILE: Ensure absolute path. Create parent directory if needed.
            if (DataFileName == null)
                DataFileName = DefaultDataFileName;
            if (!Path.IsPathRooted(DataFileName))
                DataFileName = Path.GetFullPath(Path.Combine(OutputPath, DataFileName));

            // INDEX FILE: Ensure absolute path. Create parent directory if needed.
            if (IndexFileName == null)
                IndexFileName = DefaultIndexFileName;
            if (!Path.IsPathRooted(IndexFileName))
                IndexFileName = Path.GetFullPath(Path.Combine(OutputPath, IndexFileName));

            // NAMESPACES AND CLASS NAMES
            // Split database typename to namespace and class name.
            if (DatabaseTypeName == null)
                DatabaseTypeName = Path.GetFileNameWithoutExtension(DataFileName);

            var segments = DatabaseTypeName.Split('.');
            if (segments.Length > 1)
            {
                DatabaseClassName = segments.Last();
                DatabaseNamespace = string.Join(".", segments.Take(segments.Length - 1));
            }
            else
            {
                DatabaseClassName = segments[0];
            }

            // Split index typename to namespace and class name.
            if (IndexTypeName == null)
                IndexTypeName = Path.GetFileNameWithoutExtension(IndexFileName);

            segments = IndexTypeName.Split('.');
            if (segments.Length > 1)
            {
                IndexClassName = segments.Last();
                IndexNamespace = string.Join(".", segments.Take(segments.Length - 1));
            }
            else
            {
                IndexClassName = segments[0];
            }

            // Ensure namespaces
            if (DatabaseNamespace == null)
                DatabaseNamespace = IndexNamespace;
            if (DatabaseNamespace == null)
                DatabaseNamespace = DefaultNamespace;
            if (IndexNamespace == null)
                IndexNamespace = DatabaseNamespace;

        }
        private void PrepareFileSystemEntries()
        {
            // Check whether the output path is an existing file.
            if (System.IO.File.Exists(OutputPath))
                throw new ArgumentException("Invalid output path. Output need to be a directory path.");

            // Clean existing directory or create a new one
            if (Directory.Exists(OutputPath))
            {
                System.IO.DirectoryInfo directory = new DirectoryInfo(OutputPath);
                foreach (FileInfo file in directory.GetFiles())
                    file.Delete();
                foreach (DirectoryInfo subDir in directory.GetDirectories())
                    subDir.Delete(true);
            }
            else
            {
                Directory.CreateDirectory(OutputPath);
            }

            // DATA FILE: Create parent directory if needed.
            var parentPath = Path.GetDirectoryName(DataFileName);
            if (!Directory.Exists(parentPath))
                Directory.CreateDirectory(parentPath);

            // INDEX FILE: Create parent directory if needed.
            parentPath = Path.GetDirectoryName(IndexFileName);
            if (!Directory.Exists(parentPath))
                Directory.CreateDirectory(parentPath);

            // SKIP FILE
            if (!string.IsNullOrEmpty(SkipFile))
            {
                var lines = new List<string>();
                using (var reader = new StreamReader(SkipFile))
                {
                    string path = null;
                    while ((path = reader.ReadLine()) != null)
                    {
                        if (!Path.IsPathRooted(path))
                            path = Path.GetFullPath(Path.Combine(ImportPath, path));
                        if (!path.StartsWith(ImportPath, StringComparison.CurrentCultureIgnoreCase))
                            throw new ArgumentException("One or more paths are out of the ImportPath");

                        if(!(path.Equals(ImportPath, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            path = path.Substring(0, ImportPath.Length).Replace("\\", "/");
                            lines.Add("/Root/" + path);
                        }
                    }
                }

                SkippedPathArray = lines.Union(SkippedPathArray).Distinct().ToArray();
            }
        }
    }
}
