using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class RepositoryEnvironment : SnConfig
    {
        private const string SectionName = "sensenet/repositoryEnvironment";

        public static bool BackwardCompatibilityDefaultValues { get; internal set; } = GetValue<bool>(SectionName, "BackwardCompatibilityDefaultValues");
        public static bool BackwardCompatibilityXmlNamespaces { get; internal set; } = GetValue<bool>(SectionName, "BackwardCompatibilityXmlNamespaces");
        public static bool XsltRenderingWithContentSerialization { get; internal set; } = GetValue<bool>(SectionName, "XsltRenderingWithContentSerialization", true);
        public static string FallbackCulture { get; internal set; } = GetString(SectionName, "FallbackCulture", "en");

        #region Working mode

        public static WorkingModeFlags WorkingMode { get; internal set; } = new WorkingModeFlagsNotInitialized();

        public class WorkingModeFlags
        {
            public virtual bool Populating { get; set; }
            public virtual bool Importing { get; internal set; }
            public virtual bool Exporting { get; internal set; }
            public virtual bool SnAdmin { get; internal set; }
            public virtual string RawValue { get; internal set; }

            public void SetImporting(bool value)
            {
                if (!SnAdmin)
                    throw new InvalidOperationException("The property 'Importing' is read only.");
                Importing = value;
            }
            public void SetExporting(bool value)
            {
                if (!SnAdmin)
                    throw new InvalidOperationException("The property 'Exporting' is read only.");
                Exporting = value;
            }
        }
        internal class WorkingModeFlagsNotInitialized : WorkingModeFlags
        {
            public override bool Populating
            {
                get
                {
                    Initialize();
                    return WorkingMode.Populating;
                }
                set
                {
                    Initialize();
                    WorkingMode.Populating = value;
                }
            }
            public override bool Importing
            {
                get
                {
                    Initialize();
                    return WorkingMode.Importing;
                }
                internal set
                {
                    Initialize();
                    WorkingMode.Importing = value;
                }
            }
            public override bool Exporting
            {
                get
                {
                    Initialize();
                    return WorkingMode.Exporting;
                }
                internal set
                {
                    Initialize();
                    WorkingMode.Exporting = value;
                }
            }
            public override bool SnAdmin
            {
                get
                {
                    Initialize();
                    return WorkingMode.SnAdmin;
                }
                internal set
                {
                    Initialize();
                    WorkingMode.SnAdmin = value;
                }
            }
            public override string RawValue
            {
                get
                {
                    Initialize();
                    return WorkingMode.RawValue;
                }
                internal set
                {
                    Initialize();
                    WorkingMode.RawValue = value;
                }
            }

            private static void Initialize()
            {
                var wm = new WorkingModeFlags();
                var specialWorkingMode = GetString(SectionName, "SpecialWorkingMode", string.Empty);
                if (!string.IsNullOrEmpty(specialWorkingMode))
                {
                    wm = new WorkingModeFlags
                    {
                        Populating = specialWorkingMode.IndexOf("Populating", StringComparison.OrdinalIgnoreCase) >= 0,
                        Importing = specialWorkingMode.IndexOf("Import", StringComparison.OrdinalIgnoreCase) >= 0,
                        Exporting = specialWorkingMode.IndexOf("Export", StringComparison.OrdinalIgnoreCase) >= 0,
                        SnAdmin = specialWorkingMode.IndexOf("SnAdmin", StringComparison.OrdinalIgnoreCase) >= 0,
                        RawValue = specialWorkingMode
                    };
                }

                WorkingMode = wm;
            }
        }

        #endregion

        public static List<string> DisabledNodeObservers { get; internal set; } = GetListOrEmpty<string>(SectionName, "DisabledNodeObservers");

        public static bool SkipBinaryImportIfFileDoesNotExist { get; internal set; } = GetValue<bool>(SectionName, "SkipBinaryImportIfFileDoesNotExist");
        public static bool SkipImportingMissingReferences { get; internal set; } = GetValue<bool>(SectionName, "SkipImportingMissingReferences");
        public static string[] SkipReferenceNames { get; internal set; } = { "CreatedBy", "ModifiedBy" };

        public static int DefaultLockTimeout { get; internal set; } = GetInt(SectionName, "DefaultLockTimeout", 10000000);
    }
}
