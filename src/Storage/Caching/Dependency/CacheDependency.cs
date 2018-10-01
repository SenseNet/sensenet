namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Base class of the object that declares various, implementation-independent cache dependency value.
    /// </summary>
    public class CacheDependency
    {
        internal static object EventSync = new object();
    }
}
