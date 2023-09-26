using System.Threading.Tasks;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public interface IReplicationService
{
    Task ReplicateNodeAsync(Node source, Node target, ReplicationDescriptor replicationDescriptor, CancellationToken cancel);
}
