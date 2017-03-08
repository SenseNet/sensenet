using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.Diagnostics;
using SenseNet.ContentRepository.Storage;
using System.Net;

namespace SenseNet.Communication.Messaging
{
    [Serializable]
    public class ClusterMemberInfo
    {
        public string ClusterID;
        public string ClusterMemberID;
        public string InstanceID;
        public string Machine;
        public bool NeedToRecover = true;

        private static ClusterMemberInfo _current;
        private static object _syncRoot = new object();
        public static ClusterMemberInfo Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_syncRoot)
                    {
                        if (_current == null)
                        {
                            var current = new ClusterMemberInfo();
                            current.InstanceID = Guid.NewGuid().ToString();
                            current.Machine = GetIpAddress();
                            _current = current;
                            SnLog.WriteInformation("ClusterMemberInfo created.", EventId.RepositoryLifecycle,
                                properties: new Dictionary<string, object> {{"InstanceID", _current.InstanceID}});
                        }
                    }
                }
                return _current;
            }
        }

        public bool IsMe
        {
            get
            {
                return this.InstanceID == ClusterMemberInfo.Current.InstanceID;
            }
        }

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