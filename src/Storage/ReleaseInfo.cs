using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.Storage
{
    public class ReleaseInfo
    {
        public string ProductName { get; set; }
        public string DisplayName { get; set; }
        public Version Version { get; set; }
        public DateTime ReleaseDate { get; set; }
    }

}
