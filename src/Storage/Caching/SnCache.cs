using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Caching.Dependency;

namespace SenseNet.ContentRepository.Storage.Caching
{
    internal class SnCache
    {
        public static object EventSync = new object();

        public static readonly EventServer<int> NodeIdChanged = new EventServer<int>(Cache.NodeIdDependencyEventPartitions);
        public static readonly EventServer<int> NodeTypeChanged = new EventServer<int>(Cache.NodeTypeDependencyEventPartitions);
        public static readonly EventServer<string> PathChanged = new EventServer<string>(Cache.PathDependencyEventPartitions);
        public static readonly EventServer<string> PortletChanged = new EventServer<string>(Cache.PortletDependencyEventPartitions);
    }
}
