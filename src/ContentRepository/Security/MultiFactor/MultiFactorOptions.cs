using SenseNet.Tools.Configuration;

namespace SenseNet.ContentRepository.Security.MultiFactor
{
    [OptionsClass(sectionName: "sensenet:Authentication:MultiFactor")]
    public class MultiFactorOptions
    {
        public string ApplicationName { get; set; }
        public int MaxKeyLength { get; set; } = 30;
        public int PixelsPerModule { get; set; } = 3;
        public int TimeToleranceMinutes { get; set; } = 2;
    }
}
