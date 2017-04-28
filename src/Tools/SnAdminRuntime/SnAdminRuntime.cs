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
using SenseNet.ContentRepository.Storage;
using SenseNet.Tools.SnAdmin.Testability;

namespace SenseNet.Tools.SnAdmin
{
    internal class SnAdminRuntime
    {
        #region Constants

        private static string CR = Environment.NewLine;

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

        private static string __toolTitle;

        private static string ToolTitle
        {
            get
            {
                if (__toolTitle == null)
                    __toolTitle = $"Sense/Net Admin Runtime {Assembly.GetExecutingAssembly().GetName().Version}";
                return __toolTitle;
            }
        }

        #endregion

        internal static TextWriter Output { get; set; } = Console.Out;

        internal static int Main(string[] args)
        {
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

            if (
                !ParseParameters(args, out packagePath, out targetDirectory, out phase, out parameters, out logFilePath,
                    out logLevel, out help, out schema, out wait))
                return -1;

            Logger.PackageName = Path.GetFileName(packagePath);
            try
            {
                Logger.Create(logLevel, logFilePath);
                Debug.WriteLine("##> " + Logger.Level);
                return ExecutePhase(packagePath, targetDirectory, phase, parameters, logFilePath, help, schema);
            }
            catch (System.Reflection.ReflectionTypeLoadException rtlex)
            {
                List<string> types = new List<string>();
                foreach (var item in rtlex.LoaderExceptions) //LoaderExceptions is null? 
                {
                    if(item is System.IO.FileLoadException) //namespace-eket törüljük ki
                    {
                        var flo = item as System.IO.FileLoadException;
                        types.Add(flo.FileName);
                    }
                }
                throw new Exception(string.Format("ReflectionTypeLoadException: Could not load types. Affected types: "+Environment.NewLine+ string.Join(";"+Environment.NewLine, types)+";" +Environment.NewLine));
            }
           
           
        }

        internal static bool ParseParameters(string[] args, out string packagePath, out string targetDirectory,
            out int phase, out string[] parameters, out string logFilePath, out LogLevel logLevel, out bool help,
            out bool schema, out bool wait)
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
                        case "?":
                            help = true;
                            break;
                        case "HELP":
                            help = true;
                            break;
                        case "SCHEMA":
                            schema = true;
                            break;
                        case "WAIT":
                            wait = true;
                            break;
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
                    logLevel = (LogLevel) Enum.Parse(typeof(LogLevel), arg.Substring(9));
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
                targetDirectory = Disk.SearchTargetDirectory();
            parameters = prms.ToArray();
            return true;
        }

        internal static int ExecutePhase(string packagePath, string targetDirectory, int phase, string[] parameters,
            string logFilePath, bool help, bool schema)
        {
            Logger.LogTitle(ToolTitle);
            var typeResolver = TypeResolverWrapper.Instance;

            // preload all assemblies from the sandbox folder so that we have every dll in memory
            typeResolver.LoadAssembliesFrom(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            var packageCustomizationPath = Path.Combine(packagePath, "PackageCustomization");
            if (Disk.DirectoryExists(packageCustomizationPath))
            {
                Output.WriteLine("Loading package customizations:");
                var loaded = typeResolver.LoadAssembliesFrom(packageCustomizationPath);
                foreach (var item in loaded)
                {
                    Output.Write("  ");
                    Output.WriteLine(item);
                }
            }

            var phaseCustomizationPath = GetPhaseCustomizationPath(packagePath, phase);
            if (Disk.DirectoryExists(phaseCustomizationPath))
            {
                Output.WriteLine($"Loading phase-{phase + 1} customizations:");
                var loaded = typeResolver.LoadAssembliesFrom(phaseCustomizationPath);
                foreach (var item in loaded)
                {
                    Output.Write("  ");
                    Output.WriteLine(item);
                }
            }

            if (help)
            {
                LogAssemblies();
                Logger.LogMessage(Environment.NewLine + PackageManagerWrapper.Instance.GetHelp());
                var sb = new StringBuilder();
                return 0;
            }
            if (schema)
            {
                var xsd = PackageManagerWrapper.Instance.GetXmlSchema();
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
                result = PackageManagerWrapper.Instance.Execute(packagePath, targetDirectory, phase, parameters, Output);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }
            finally
            {
                // Stop this background thread so that the app could exit correctly. This is a
                // workaround for cases when the Repository was not started during execution,
                // but the clusterchannel started because one of the components needed it.
                DistributedApplication.ClusterChannel.ShutDown();
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
                return 1 + Logger.Errors*2;
            return Logger.Errors*2;
        }

        private static string GetPhaseCustomizationPath(string packagePath, int phase)
        {
            var files = Disk.GetFiles(packagePath);
            if (files.Length != 1)
                return null;

            XmlDocument xml;
            try
            {
                xml = Disk.LoadManifest(files[0]);
            }
            catch (Exception e)
            {
                return null;
            }

            var phaseElement = (XmlElement) xml?.DocumentElement?.SelectSingleNode($"Steps/Phase[{phase + 1}]");

            var relPath = phaseElement?.Attributes["extensions"]?.Value;

            return relPath == null ? null : Path.Combine(packagePath, relPath);
        }

        private static void LogAssemblies()
        {
            Logger.LogMessage("Assemblies:");
            foreach (var asm in SenseNet.ContentRepository.Storage.TypeHandler.GetAssemblyInfo())
                Logger.LogMessage("  {0} {1}", asm.Name, asm.Version);
        }
    }
}
