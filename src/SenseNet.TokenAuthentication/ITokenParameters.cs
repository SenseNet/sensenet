using System;

namespace SenseNet.TokenAuthentication
{
    public interface ITokenParameters
    {
        string Issuer { get; set; }
        string Subject { get; set; }
        string Audience { get; set; }
        string EncryptionAlgorithm { get; set; }
        int AccessLifeTimeInMinutes { get; set; }
        int RefreshLifeTimeInMinutes { get; set; }
        bool ValidateLifeTime { get; set; }
        int ClockSkewInMinutes { get; set; }
        DateTime? ValidFrom { get; set; }
    }
}