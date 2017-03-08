using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ApplicationModel
{
    public class ActionIcon
    {
        public string IconName { get; set; }
        
        private string _iconUrl;
        public string IconUrl
        {
            get
            {
                return _iconUrl ?? "/Root/Global/images/icons/16/" + IconName + ".png"; ;
            }
            set
            {
                _iconUrl = value;
            }
        }
    }
}
