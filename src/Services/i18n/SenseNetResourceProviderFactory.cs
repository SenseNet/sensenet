using System.Web.Compilation;

namespace SenseNet.ContentRepository.i18n
{
    public class SenseNetResourceProviderFactory : ResourceProviderFactory
    {
        /// <summary>
        /// Creates a resourceprovider with the specified classkey.
        /// </summary>
        /// <param name="classKey">Classkey holds the name of the resourcekey.</param>
		/// <returns>New SenseNetResourceProvider instance.</returns>
        public override IResourceProvider CreateGlobalResourceProvider(string classKey)
        {
            return new SenseNetResourceProvider(classKey);
        }
        /// <summary>
        /// Creates a resourceprovider with the specified virtualpath.
        /// </summary>
        /// <param name="virtualPath">Virtualpath holds the name of the virtualpath which is localized.</param>
		/// <returns>New SenseNetResourceProvider instance.</returns>
        public override IResourceProvider CreateLocalResourceProvider(string virtualPath)
        {
            return new SenseNetResourceProvider(virtualPath);
        }
    }
}