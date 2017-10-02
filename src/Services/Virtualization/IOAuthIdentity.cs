using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Services.Virtualization
{
    public interface IOAuthIdentity
    {
        string Username { get; }
        string Identifier { get; }
        string Email { get; }
    }

    public class OAuthIdentity : IOAuthIdentity
    {
        public string Username { get; set; }
        public string Identifier { get; set; }
        public string Email { get; set; }
    }
}
