using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using SenseNet.Packaging;
using SenseNet.ContentRepository;
using System.Diagnostics;
using Ionic.Zip;
using System.Configuration;
using System.Xml;

namespace SenseNet.Tools.SnAdmin
{
    internal class SnAdminRuntime
    {
        #region Constants
        private static string CR = Environment.NewLine;
        private static string ToolTitle = "Sense/Net Admin Runtime ";
        private static string UsageScreen = String.Concat(
            //         1         2         3         4         5         6         7         8
            // 2345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
            "DO NOT RUN THIS TOOL DIRECTLY. This tool a part of the SnAdmin workflow.", CR,
            CR,
            "SnAdmin Usage:", CR,
            "SnAdmin <package> [<target>]", CR,
            CR,
            "Parameters:", CR,
            "<package>: File contains a package (*.zip or directory).", CR,
            "<target>: Directory contains web folder of a stopped SenseNet instance.", CR
        );

        #endregion

        private static int Main(string[] args)
        {
            ToolTitle += Assembly.GetExecutingAssembly().GetName().Version;
            if (args.FirstOrDefault(a => a.ToUpper() == "-WAIT") != null)
            {
                Console.WriteLine("Running in wait mode - now you can attach to the process with a debugger.");
                Console.WriteLine("Press ENTER to continue.");
                Console.ReadLine();
            }

            string packagePath;
            string targetDirectory;
            int phase = -1;
            string logFilePath;
            LogLevel logLevel;
            bool help;
            bool schema;
            bool wait;
            string[] parameters;

            if (!ParseParameters(args, out packagePath, out targetDirectory, out phase, out parameters, out logFilePath, out logLevel, out help, out schema, out wait))
                return -1;

            Logger.PackageName = Path.GetFileName(packagePath);

            Logger.Create(logLevel, logFilePath);
            Debug.WriteLine("##> " + Logger.Level);

            return ExecutePhase(packagePath, targetDirectory, phase, parameters, logFilePath, help, schema);
        }
        private static bool ParseParameters(string[] args, out string packagePath, out string targetDirectory, out int phase, out string[] parameters, out string logFilePath, out LogLevel logLevel, out bool help, out bool schema, out bool wait)
        {
            packagePath = null;
            targetDirectory = null;
            phase = -1;
            logFilePath = null;
            wait = false;
            help = false;
            schema = false;
            logLevel = LogLevel.Default;
            var prms = new List<string>();
            var argIndex = -1;

            foreach (var arg in args)
            {
                argIndex++;

                if (arg.StartsWith("-"))
                {
                    var verb = arg.Substring(1).ToUpper();
                    switch (verb)
                    {
                        case "?": help = true; break;
                        case "HELP": help = true; break;
                        case "SCHEMA": schema = true; break;
                        case "WAIT": wait = true; break;
                    }
                }
                else if (arg.StartsWith("PHASE:", StringComparison.OrdinalIgnoreCase))
                {
                    phase = int.Parse(arg.Substring(6));
                }
                else if (arg.StartsWith("LOG:", StringComparison.OrdinalIgnoreCase))
                {
                    logFilePath = arg.Substring(4);
                }
                else if (arg.StartsWith("LOGLEVEL:", StringComparison.OrdinalIgnoreCase))
                {
                    logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), arg.Substring(9));
                }
                else if (arg.StartsWith("TARGETDIRECTORY:", StringComparison.OrdinalIgnoreCase))
                {
                    targetDirectory = arg.Substring(16).Trim('"');
                }
                else if (PackageParameter.IsValidParameter(arg) && argIndex > 0)
                {
                    // Recognise this as a 'parameter' only if it is not the first one
                    // (which must be the package path without a param name prefix).
                    prms.Add(arg);
                }
                else if (packagePath == null)
                {
                    packagePath = arg;
                }
            }
            if (targetDirectory == null)
                targetDirectory = SearchTargetDirectory();
            parameters = prms.ToArray();
            return true;
        }

        private static int ExecutePhase(string packagePath, string targetDirectory, int phase, string[] parameters, string logFilePath, bool help, bool schema)
        {
            Logger.LogTitle(ToolTitle);

            // preload all assemblies from the sandbox folder so that we have every dll in memory
            TypeResolver.LoadAssembliesFrom(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            var packageCustomizationPath = Path.Combine(packagePath, "PackageCustomization");
            if (Directory.Exists(packageCustomizationPath))
            {
                Console.WriteLine("Loading package customizations:");
                var loaded = TypeResolver.LoadAssembliesFrom(packageCustomizationPath);
                foreach (var item in loaded)
                {
                    Console.Write("  ");
                    Console.WriteLine(item);
                }
            }

            if (help)
            {
                LogAssemblies();
                Logger.LogMessage(Environment.NewLine + PackageManager.GetHelp());
                var sb = new StringBuilder();
                return 0;
            }
            if (schema)
            {
                var xsd = PackageManager.GetXmlSchema();
                Logger.LogMessage(Environment.NewLine + xsd);
                var xsdPath = Path.GetFullPath(packagePath + @"\..\bin\SenseNetPackage.xsd");

                using (var writer = new StreamWriter(xsdPath, false))
                    writer.Write(xsd);

                Logger.LogMessage("XSD is written to " + xsdPath);

                return 0;
            }

            PackagingResult result = null;
            try
            {
                result = PackageManager.Execute(packagePath, targetDirectory, phase, parameters, Console.Out);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }

            // result:
            // -2: error,
            // -1: terminated,
            // 0: successful with no errors,
            // 1: need restart,
            // 2: (not used)
            // 3: 1 error
            // 4: 1 error and restart
            // n: (n-1)/2 errors plus 1 if restart
            if (result == null)
                return -2;
            if (!result.Successful)
                return result.Terminated ? -1 : -2;
            if (result.NeedRestart)
                return 1 + Logger.Errors * 2;
            return Logger.Errors * 2;
        }
        private static void LogAssemblies()
        {
            Logger.LogMessage("Assemblies:");
            foreach (var asm in SenseNet.ContentRepository.Storage.TypeHandler.GetAssemblyInfo())
                Logger.LogMessage("  {0} {1}", asm.Name, asm.Version);
        }

        private static string SearchTargetDirectory()
        {
            if (!string.IsNullOrEmpty(Configuration.Packaging.TargetDirectory))
                return Configuration.Packaging.TargetDirectory;

            // default location: ..\webfolder\Admin\bin
            var workerExe = Assembly.GetExecutingAssembly().Location;
            var path = workerExe;

            // go up on the parent chain
            path = Path.GetDirectoryName(path);
            path = Path.GetDirectoryName(path);

            // get the name of the container directory (should be 'Admin')
            var adminDirName = Path.GetFileName(path);
            path = Path.GetDirectoryName(path);

            if (string.Compare(adminDirName, "Admin", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // look for the web.config
                if (System.IO.File.Exists(Path.Combine(path, "web.config")))
                    return path;
            }
            throw new ApplicationException("Configure the TargetDirectory. This path does not exist or it is not a valid target: " + path);
        }
    }
}
