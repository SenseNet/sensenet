namespace SenseNet.ContentRepository.Search.Indexing
{
    //UNDONE:!!!! XMLDOC ContentRepository
    public interface IIndexValueConverter<T>
    {
        //UNDONE:!!!! XMLDOC ContentRepository
        T GetBack(string fieldValue);
    }
    //UNDONE:!!!! XMLDOC ContentRepository
    public interface IIndexValueConverter
    {
        //UNDONE:!!!! XMLDOC ContentRepository
        object GetBack(string fieldValue);
    }
}
