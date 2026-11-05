using System;

namespace SenseNet.Services.Core.Authentication
{
    /// <summary>A password check was rejected before evaluating the password.</summary>
    public sealed class PasswordValidationThrottledException : Exception
    {
        public int RetryAfterSeconds { get; }

        public PasswordValidationThrottledException(int retryAfterSeconds)
            : base("Too many authentication attempts. Try again later.")
        {
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}
