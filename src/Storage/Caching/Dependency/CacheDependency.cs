namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    /// <summary>
    /// Base class for various implementation-independent cache dependency types.
    /// </summary>
    public class CacheDependency
    {
        internal static object EventSync = new object();
    }
}
