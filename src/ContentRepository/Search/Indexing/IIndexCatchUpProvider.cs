using System.IO;
using System.Threading;
using SenseNet.Search.Indexing;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal interface IIndexCatchUpProvider
    {
        // Prepare the queue's completion and arrival state before startup continues.
        STT.Task CatchUpAsync(DistributedIndexingActivityQueue queue, IndexingActivityStatus status,
            TextWriter consoleOut, CancellationToken cancellationToken);
    }
}
