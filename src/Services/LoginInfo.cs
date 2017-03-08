using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Portal
{
    public class LoginInfo
    {
        public string UserName { get; set; }
        public string Message { get; set; }
    }
    public class CancellableLoginInfo : LoginInfo
    {
        private bool _cancel;
        public bool Cancel
        {
            get { return _cancel; }
            set { _cancel |= value; }
        }
    }
}
