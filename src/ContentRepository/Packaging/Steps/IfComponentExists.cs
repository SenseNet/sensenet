using System;
using SenseNet.ContentRepository;

namespace SenseNet.Packaging.Steps
{
    public class IfComponentExists : IfCondition
    {
        public override string ElementName => GetType().Name;

        public string Id { get; set; }
        public string MinVersion { get; set; }
        public string MaxVersion { get; set; }
        public bool MinVersionIsExclusive { get; set; }
        public bool MaxVersionIsExclusive { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidStepParameterException("Component id is missing.");
            if (MinVersion == null && MaxVersion == null)
                throw new InvalidStepParameterException("Please provider either the minversion or the maxversion attribute.");

            Version minVersion = null;
            Version maxVersion = null;

            if (MinVersion != null && !Version.TryParse(MinVersion, out minVersion))
                throw new InvalidStepParameterException("MinVersion is invalid.");
            if (MaxVersion != null && !Version.TryParse(MaxVersion, out maxVersion))
                throw new InvalidStepParameterException("MaxVersion is invalid.");

            var dependency = new Dependency
            {
                Id = Id,
                MinVersion = minVersion,
                MaxVersion = maxVersion,
                MinVersionIsExclusive = MinVersionIsExclusive,
                MaxVersionIsExclusive = MaxVersionIsExclusive
            };

            try
            {
                context.Manifest.CheckDependency(dependency, RepositoryVersionInfo.Instance, false);
                return true;
            }
            catch(PackagePreconditionException)
            {
                return false;
            }
        }
    }
}
