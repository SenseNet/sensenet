using SenseNet.ContentRepository;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class Versioning : SnConfig
    {
        private const string SectionName = "sensenet/versioning";

        public static CheckInCommentsMode CheckInCommentsMode { get; internal set; } = GetValue<CheckInCommentsMode>(SectionName, "CheckInComments", CheckInCommentsMode.Recommended);
    }
}
