using System.IO;

namespace SenseNet.Search
{
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
    }
}
