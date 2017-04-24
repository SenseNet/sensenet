using System;

namespace SenseNet.TokenAuthentication
{
    public class TokenParameters: ITokenParameters
    {
        public string Issuer { get; set; }
        public string Subject { get; set; }
        public string Audience { get; set; }
        public string EncryptionAlgorithm { get; set; }
        public int AccessLifeTimeInMinutes { get; set; }
        public int RefreshLifeTimeInMinutes { get; set; }
        public bool ValidateLifeTime { get; set; }
        public int ClockSkewInMinutes { get; set; }
        public DateTime? ValidFrom { get; set; }
    }
}