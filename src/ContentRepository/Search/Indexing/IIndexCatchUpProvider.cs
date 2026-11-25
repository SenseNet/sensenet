using System.IO;
using System.Threading;
using SenseNet.Search.Indexing;
using STT = System.Threading.Tasks;

namespace SenseNet.ContentRepository.Search.Indexing
{
    internal interface IIndexCatchUpProvider
    {
        bool BufferLiveActivities => false;

        // Prepare the queue's completion and arrival state before startup continues.
        STT.Task CatchUpAsync(IndexManager indexManager, IndexingActivityStatus status,
            TextWriter consoleOut, CancellationToken cancellationToken);
    }
}
