using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    public interface IResourceManager
    {
        bool Running { get; }
        string GetString(string fullResourceKey);
        string GetString(string fullResourceKey, params object[] args);
        bool ParseResourceKey(string source, out string className, out string name);
        IEnumerable<string> GetResourceFilesForClass(string className);
    }

    internal class DefaultResourceManager : IResourceManager
    {
        public bool Running { get { return true; } }

        public string GetString(string fullResourceKey)
        {
            return fullResourceKey;
        }
        public string GetString(string fullResourceKey, params object[] args)
        {
            return String.Concat(fullResourceKey, ": ", String.Join(", ", args.Select(a => a.ToString())));
        }

        public bool ParseResourceKey(string source, out string className, out string name)
        {
            className = string.Empty;
            name = string.Empty;

            return false;
        }

        public IEnumerable<string> GetResourceFilesForClass(string className)
        {
            return new List<string>();
        }
    }
}
