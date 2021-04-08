using System.Collections.Generic;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Services.Core.Operations
{
    public class RepositoryVersionView
    {
        public IEnumerable<SnComponentView> Components { get; set; }
        public AssemblyDetails Assemblies { get; set; }
        public IEnumerable<Package> InstalledPackages { get; set; }
        public bool DatabaseAvailable { get; set; }
    }
}