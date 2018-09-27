namespace SenseNet.ContentRepository.Storage.Caching.Dependency
{
    //UNDONE: Ensure that the CacheDependency is extensible (EventHolder["NodeType"].TheEvent += CallbackMethod;... EventHolder is in the Providers instance
    /// <summary>
    /// Base class of the object that declares various, implementation-independent cache dependency value.
    /// </summary>
    public class CacheDependency
    {
        internal static object EventSync = new object();
    }
}
