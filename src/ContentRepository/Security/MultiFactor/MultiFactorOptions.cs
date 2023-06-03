namespace SenseNet.ContentRepository.Security.MultiFactor
{
    public class MultiFactorOptions
    {
        public string ApplicationName { get; set; }
        public int MaxKeyLength { get; set; } = 30;
        public int PixelsPerModule { get; set; } = 3;
        public int TimeToleranceMinutes { get; set; } = 2;
    }
}
