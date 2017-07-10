namespace SenseNet.ContentRepository.Storage.Search
{
    public enum IndexingMode { Default, Analyzed, AnalyzedNoNorms, No, NotAnalyzed, NotAnalyzedNoNorms }
    public enum IndexStoringMode { Default, No, Yes }
    public enum IndexTermVector { Default, No, WithOffsets, WithPositions, WithPositionsOffsets, Yes }

    public interface ISearchEngineSupport //UNDONE:! Set an instance at system start
    {
        bool RestoreIndexOnstartup();
        int[] GetNotIndexedNodeTypeIds();
    }
}
