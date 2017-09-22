using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Communication.Messaging;
using SafeQueries = SenseNet.ContentRepository.SafeQueries;

namespace SenseNet.ApplicationModel
{
    public class DeviceManager
    {
        internal static void Reset()
        {
            new DeviceManagerResetDistributedAction().Execute();
        }
        private static void ResetPrivate()
        {
            __instance = null;
        }
        [Serializable]
        internal sealed class DeviceManagerResetDistributedAction : DistributedAction
        {
            public override void DoAction(bool onRemote, bool isFromMe)
            {
                // Local echo of my action: Return without doing anything
                if (onRemote && isFromMe)
                    return;
                DeviceManager.ResetPrivate();
            }
        }

        // ======================================================================================================== Static interface

        /// <summary>
        /// Returns identified device or null
        /// </summary>
        /// <param name="userAgent">Device will be parsed from this</param>
        /// <returns>Identified device name or null</returns>
        public static string GetRequestedDeviceName(string userAgent)
        {
            return Instance == null ? null : Instance.GetRequestedDeviceNamePrivate(userAgent);
        }
        /// <summary>
        /// Returns the device fallback chain. Empty array if requestedDevice is not registered as a SenseNet.ApplicationModel.Device.
        /// Returns an array with one item if there is no fallback device.
        /// Index of identified device is 0, first fallback is 1 and so on.
        /// </summary>
        /// <param name="requestedDevice"></param>
        /// <returns></returns>
        public static string[] GetDeviceChain(string requestedDevice)
        {
            return Instance == null ? null : Instance.GetDeviceChainPrivate(requestedDevice);
        }

        // ======================================================================================================== Singleton

        private static object _instanceLock = new object();
        private static DeviceManager __instance;
        private static DeviceManager Instance
        {
            get
            {
                if (__instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (__instance == null)
                        {
                            // install time there is no Device type yet
                            if (ActiveSchema.NodeTypes["Device"] == null)
                                return null;

                            // Elevation: initializing and caching devices
                            // should be independent from the current user.
                            using (new SystemAccount())
                            {
                                var instance = new DeviceManager();
                                instance.Initialize();
                                __instance = instance; 
                            }
                        }
                    }
                }
                return __instance;
            }
        }
        private void Initialize()
        {
            List<Device> devices;
            var sorted = new List<Device>();

            if (StorageContext.Search.ContentQueryIsAllowed)
            {
                var result = ContentQuery.Query(SafeQueries.AllDevices);
                devices = result.Nodes.Cast<Device>().ToList();
            }
            else
            {
                // query devices and sort them by Index
                devices = NodeQuery.QueryNodesByTypeAndPath(ActiveSchema.NodeTypes["Device"], false, "/Root/System/Devices", false).Nodes.Cast<Device>().ToList();
            }

            devices.Sort(new NodeComparer<Node>());

            //TODO: Device is renamed to lowercase but only in memory. Mustn't save it.
            foreach (var device in devices)
                device.Name = device.Name.ToLower();

            var removes = new List<Device>();
            foreach (var parent in devices)
            {
                foreach (var child in devices)
                {
                    if (child.ParentId == parent.Id)
                    {
                        child.Fallback = parent;
                        removes.Add(parent);
                    }
                }
            }
            removes.Sort();
            for (int i = removes.Count - 1; i >= 0; i--)
                devices.Remove(removes[i]);

            foreach (var device in devices)
            {
                var d = device;
                while (d != null)
                {
                    sorted.Add(d);
                    d = d.Fallback;
                }
            }
            _devices = sorted.ToArray();
        }

        // ======================================================================================================== Instance part

        private Device[] _devices;
        private string[] _emptyChain = new string[0];

        private string GetRequestedDeviceNamePrivate(string userAgent)
        {
            foreach (var device in _devices)
                if (device.Identify(userAgent))
                    return device.Name;
            return null;
        }
        private string[] GetDeviceChainPrivate(string requestedDevice)
        {
            Device device = null;
            for (int i = 0; i < _devices.Length; i++)
            {
                if (_devices[i].Name == requestedDevice)
                {
                    device = _devices[i];
                    break;
                }
            }
            if (device == null)
            {
                return new[] { requestedDevice };
            }

            var chain = new List<string>();
            while (device != null)
            {
                chain.Add(device.Name);
                device = device.Fallback;
            }
            return chain.ToArray();
        }

    }
}
