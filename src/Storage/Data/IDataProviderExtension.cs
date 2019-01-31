namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines a base interface for DataProvider extensions
    /// </summary>
    public interface IDataProviderExtension
    {
        /// <summary>
        /// Gets or sets the backreference of the extension provider to the base data provider.
        /// This property is written by the sensenet infrastructure.
        /// Do not write this property from client code.
        /// </summary>
        DataProvider MainProvider { get; set; }
    }
}
