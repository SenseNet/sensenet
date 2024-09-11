using SenseNet.Tools.Configuration;

namespace SenseNet.ContentRepository.Security.MultiFactor
{
    /// <summary>
    /// Options for configuring the multi-factor authentication feature.
    /// </summary>
    [OptionsClass(sectionName: "sensenet:Authentication:MultiFactor")]
    public class MultiFactorOptions
    {
        /// <summary>
        /// Application name for the multi-factor authentication feature.
        /// This value is used when connecting to the Google two-factor 
        /// authentication API.
        /// </summary>
        public string ApplicationName { get; set; }
        /// <summary>
        /// The maximum length of the key used for multi-factor authentication.
        /// Default value is 30.
        /// </summary>
        public int MaxKeyLength { get; set; } = 30;
        /// <summary>
        /// The number of pixels per module in the QR code.
        /// Default value is 3.
        /// </summary>
        public int PixelsPerModule { get; set; } = 3;
        /// <summary>
        /// The time tolerance in minutes for the multi-factor authentication.
        /// Default value is 2.
        /// </summary>
        public int TimeToleranceMinutes { get; set; } = 2;
    }
}
