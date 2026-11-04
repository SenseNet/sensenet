namespace SenseNet.Services.Core.Authentication
{
    /// <summary>Per-application limits shared by all repository password validation endpoints.</summary>
    public sealed class PasswordValidationOptions
    {
        /// <summary>Maximum password checks per resolved user in a sliding 60-second window.</summary>
        public int AttemptsPerMinutePerAccount { get; set; } = 5;
    }
}
