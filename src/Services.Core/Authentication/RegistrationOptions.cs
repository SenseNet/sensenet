using System.Collections.Generic;

namespace SenseNet.Services.Core.Authentication
{
    public class RegistrationOptions
    {
        /// <summary>
        /// A list of group ids or paths that newly registered users should be added to.
        /// </summary>
        public ICollection<string> Groups { get; set; } = new List<string>();
        /// <summary>
        /// Content type of newly created users. Default: User.
        /// </summary>
        public string UserType { get; set; }
    }
}
