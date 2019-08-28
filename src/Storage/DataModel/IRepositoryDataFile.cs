using System.Collections.Generic;
// ReSharper disable once CheckNamespace

namespace SenseNet.ContentRepository.Storage.DataModel
{
    public interface IRepositoryDataFile
    {
        string PropertyTypes { get; }
        string NodeTypes { get; }
        string Nodes { get; }
        string Versions { get; }
        string DynamicData { get; }
        IDictionary<string, string> ContentTypeDefinitions { get; }
        IDictionary<string, string> Blobs { get; }
        IList<string> Permissions { get; }
    }
}
