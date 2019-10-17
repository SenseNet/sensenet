using SenseNet.ContentRepository.Schema.Metadata;

namespace SenseNet.Services.Metadata
{
    /// <summary>
    /// Defines methods for a metadata provider that converts and possibly stores schema
    /// items in a format appropriate for the client.
    /// </summary>
    public interface IClientMetadataProvider //UNDONE:ODATA: ?? NAMESPACE: IClientMetadataProvider
    {
        /// <summary>
        /// Gets a - possibly cached - object that represents a schema class 
        /// and can be serialized and sent to the client.
        /// </summary>
        object GetClientMetaClass(Class schemaClass);
    }
}