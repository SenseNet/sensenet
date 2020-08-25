// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class EmailOptions
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string FromAddress { get; set; }
        public string SenderName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
