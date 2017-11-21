namespace SenseNet.ContentRepository.Security
{
    public interface IOAuthIdentity
    {
        string FullName { get; }
        string Username { get; }
        string Identifier { get; }
        string Email { get; }
    }

    public class OAuthIdentity : IOAuthIdentity
    {
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Identifier { get; set; }
        public string Email { get; set; }
    }
}
