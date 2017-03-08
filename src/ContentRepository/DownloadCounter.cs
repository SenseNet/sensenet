using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.ContentRepository
{
    public interface IDownloadCounter
    {
        void Increment(int fileId);
        void Increment(string filePath);
    }

    internal class DefaultDownloadCounter : IDownloadCounter
    {
        public void Increment(int fileId) { }
        public void Increment(string filePath) { }
    }

    public class DownloadCounter
    {
        private static IDownloadCounter _instance;
        private static object _lockObject = new object();

        private static IDownloadCounter Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            var instance = TypeResolver.GetTypesByInterface(typeof (IDownloadCounter))
                                .Where(t => t != typeof (DefaultDownloadCounter)).FirstOrDefault();

                            _instance = instance == null
                                            ? new DefaultDownloadCounter()
                                            : (IDownloadCounter) Activator.CreateInstance(instance);
                        }
                    }
                }

                return _instance;
            }
        }

        public static void Increment(int fileId)
        {
            if (Logging.DownloadCounterEnabled)
                Instance.Increment(fileId);
        }

        public static void Increment(string filePath)
        {
            if (Logging.DownloadCounterEnabled)
                Instance.Increment(filePath);
        }
    }
}
