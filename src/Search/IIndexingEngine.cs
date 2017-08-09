using System.IO;

namespace SenseNet.Search
{
    public interface IIndexingActivityStatus
    {
        int LastActivityId { get; set; }
        int[] Gaps { get; set; }
    }

    public interface IIndexingEngine
    {
        bool Running { get; }
        bool Paused { get; }
        void Pause();
        void Continue();
        void Start(TextWriter consoleOut);
        void WaitIfIndexingPaused();
        void ShutDown();
        void Restart();

        void ActivityFinished();
        void Commit();

        IIndexingActivityStatus ReadActivityStatusFromIndex();
    }
}
