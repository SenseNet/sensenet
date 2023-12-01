﻿using SenseNet.Tools.Configuration;
using System.Collections.Generic;

namespace SenseNet.Services.Core.Authentication
{
    [OptionsClass(sectionName: "sensenet:Registration")]
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
        /// <summary>
        /// Container of newly created users. Default: /Root/IMS/Public
        /// </summary>
        public string ParentPath { get; set; }
    }
}
