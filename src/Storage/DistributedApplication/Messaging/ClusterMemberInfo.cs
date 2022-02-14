using System;
using System.Collections.Generic;

namespace SenseNet.Communication.Messaging
{
    [Serializable]
    public class ClusterMemberInfo
    {
        public string ClusterID { get; set; }
        public string ClusterMemberID { get; set; }
        public string InstanceID { get; set; } = Guid.NewGuid().ToString();
        public string Machine { get; set; } = GetIpAddress();
        public bool NeedToRecover { get; set; } = true;

        private static Lazy<ClusterMemberInfo> _cmi = new Lazy<ClusterMemberInfo>(() => new ClusterMemberInfo());

        public static ClusterMemberInfo Current
        {
            get => _cmi.Value;
            set { _cmi = new Lazy<ClusterMemberInfo>(() => value); }
        }

        public bool IsMe => this.InstanceID == ClusterMemberInfo.Current.InstanceID;

        public static string GetIpAddress()
        {
            var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            var ipList = new List<string>();
            foreach (var adapter in nics)
                foreach (var x in adapter.GetIPProperties().UnicastAddresses)
                    if (x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        ipList.Add(x.Address.ToString());
            return ipList[0];
        }
    }
}