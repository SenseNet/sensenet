using SenseNet.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    public interface IPreviewProvider
    {
        bool HasPreviewPermission(NodeHead nodeHead);
    }

    /// <summary>
    /// This internal class was created to make the DocumentPreviewProvider feature (that resides up in the ContentRepository layer) accessible here in the Storage layer.
    /// </summary>
    internal class PreviewProvider
    {
        // ============================================================================== Static internal API
        
        /// <summary>
        /// Instance of a DocumentPreviewProvider in the Storage layer. This property is a duplicate
        /// of the Current property of the DocumentPreviewProvider class.
        /// </summary>
        private static IPreviewProvider Current => Providers.Instance.PreviewProvider;

        internal static bool HasPreviewPermission(NodeHead nodeHead)
        {
            return Current != null && Current.HasPreviewPermission(nodeHead);
        }
    }
}
