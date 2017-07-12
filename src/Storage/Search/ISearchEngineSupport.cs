namespace SenseNet.ContentRepository.Storage.Search
{
    public enum IndexingMode { Default, Analyzed, AnalyzedNoNorms, No, NotAnalyzed, NotAnalyzedNoNorms }
    public enum IndexStoringMode { Default, No, Yes }
    public enum IndexTermVector { Default, No, WithOffsets, WithPositions, WithPositionsOffsets, Yes }

    public interface ISearchEngineSupport
    {
        bool RestoreIndexOnstartup();
        int[] GetNotIndexedNodeTypeIds();
        IPerFieldIndexingInfo GetPerFieldIndexingInfo(string fieldName);
        bool IsContentTypeIndexed(string contentTypeName);
        bool TextExtractingWillBePotentiallySlow(IIndexableField field);
        string ReplaceQueryTemplates(string luceneQueryText);
    }
}
