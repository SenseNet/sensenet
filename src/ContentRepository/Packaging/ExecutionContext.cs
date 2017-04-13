using System.Collections.Generic;
using System.Linq;
using System.IO;
using SenseNet.ContentRepository;

namespace SenseNet.Packaging
{
    public enum TerminationReason { Successful, Warning }

    /// <summary>Contains package information for executing a step.</summary>
    public class ExecutionContext
    {
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>();

        /// <summary>Returns a named value that was memorized in the current phase.</summary>
        public object GetVariable(string name)
        {
            if (name == null)
                return null;

            object result;
            if (_variables.TryGetValue(name.ToLowerInvariant(), out result))
                return result;
            return null;
        }
        /// <summary>Memorize a named value at the end of the current phase.</summary>
        public void SetVariable(string name, object value)
        {
            _variables[name.ToLowerInvariant()] = value;
        }

        /// <summary>Fully qualified path of the executing extracted package.</summary>
        public string PackagePath { get; private set; }
        /// <summary>Fully qualified path of the executing extracted package.</summary>
        public string TargetPath { get; private set; }
        /// <summary>UNC paths of the related network server web directories.</summary>
        public string[] NetworkTargets { get; private set; }
        /// <summary>Fully qualified path of the phase executor assemblies directory.</summary>
        public string SandboxPath { get; private set; }
        /// <summary>Parsed manifest.</summary>
        public Manifest Manifest { get; private set; }
        /// <summary>Zero based index of the executing phase.</summary>
        public int CurrentPhase { get; private set; }
        /// <summary>Phase count of the currently executed package.</summary>
        public int CountOfPhases { get; private set; }
        /// <summary>Console out of the executing SnAdmin. Write here any information that you do not want to log.</summary>
        public TextWriter Console { get; private set; }
        /// <summary>True if the StartRepository step has already executed.</summary>
        public bool RepositoryStarted { get; internal set; }
        internal bool Test { get; set; }

        /// <summary>
        /// DO NOT USE THIS CONSTRUCTOR FROM TESTS. Use ExecutionContext.CreateForTest method instead.
        /// </summary>
        internal ExecutionContext(string packagePath, string targetPath, string[] networkTargets, string sandboxPath, Manifest manifest, int currentPhase, int countOfPhases, PackageParameter[] packageParameters, TextWriter console)
        {
            this.PackagePath = packagePath;
            this.TargetPath = targetPath;
            this.NetworkTargets = networkTargets;
            this.SandboxPath = sandboxPath;
            this.Manifest = manifest;
            this.CurrentPhase = currentPhase;
            this.CountOfPhases = countOfPhases;
            this.Console = console;

            if (manifest == null)
                return;

            foreach (var manifestParameter in manifest.Parameters)
            {
                var name = manifestParameter.Key;
                var propertyName = name.TrimStart('@');
                var defaultValue = manifestParameter.Value;
                var value = packageParameters.FirstOrDefault(p => p.PropertyName.ToLowerInvariant() == propertyName)?.Value;

                SetVariable(name, value ?? defaultValue);
            }
        }
        internal static ExecutionContext CreateForTest(string packagePath, string targetPath, string[] networkTargets, string sandboxPath, Manifest manifest, int currentPhase, int countOfPhases, string[] parameters, TextWriter console)
        {
            var packageParameters = parameters?.Select(PackageParameter.Parse).ToArray() ?? new PackageParameter[0];
            return new ExecutionContext(packagePath, targetPath, networkTargets, sandboxPath, manifest, currentPhase, countOfPhases, packageParameters, console) { Test = true };
        }

        /// <summary>Verifies that the Repository is running and throws an exception if not.</summary>
        public void AssertRepositoryStarted()
        {
            if (!RepositoryStarted)
                throw new PackagingException("Please start the repository before executing this step using the StartRepository step.");
        }

        public string TerminationMessage { get; private set; }
        public TerminationReason TerminationReason { get; private set; }
        public bool Terminated { get { return TerminationMessage != null; } }
        public int TerminatorStepId { get; private set; }
        public void TerminateExecution(string message, TerminationReason reason, Steps.Step terminatorStep)
        {
            this.TerminationMessage = message;
            this.TerminationReason = reason;
            this.TerminatorStepId = terminatorStep.StepId;
        }

        public object ResolveVariable(string text)
        {
            if (text == null || text.StartsWith("@@") || !text.StartsWith("@"))
                return text;
            var src = text.Split('.');
            var obj = GetVariable(src[0]);
            for (int i = 1; i < src.Length; i++)
            {
                if (obj == null)
                    return null;
                obj = GetFieldOrProperty(obj, src[i]);
            }
            return obj;
        }

        private object GetFieldOrProperty(object obj, string name)
        {
            var type = obj.GetType();
            var fieldInfo = type.GetField(name);
            if (fieldInfo != null)
                return fieldInfo.GetValue(obj);

            var propertyInfo = type.GetProperty(name);
            if (propertyInfo != null)
                return propertyInfo.GetValue(obj);

            var content = obj as Content;
            if (content != null)
                return content[name];

            //TODO: more conversions can be here

            return null;
        }

        internal void LogVariables()
        {
            if (_variables.Count == 0)
                return;

            Logger.LogMessage("Variables:");
            foreach (var variable in _variables)
            {
                Logger.LogMessage($"   {variable.Key}: {variable.Value}");
            }
        }
    }
}
