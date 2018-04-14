using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Tests
{
    public static class Extensions
    {
        /// <summary>
        /// Returns with a new child of the given parent by the specified content type and name.
        /// The output and return values are reference equal.
        /// This method is helps to create a content chain and uses this link in a local variable:
        /// .CreateChild("MyType", "Content-1", , out Node localNode);
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="typeName">Name of the existing content type.</param>
        /// <param name="name">Name of the new content.</param>
        /// <param name="child">The new child content. Copy of the return value.</param>
        /// <returns>The new child content.</returns>
        public static Node CreateChild(this Node parent, string typeName, string name, out Node child)
        {
            child = parent.CreateChild(typeName, name);
            return child;
        }
        /// <summary>
        /// Returns with a new child of the given parent by the specified content type and name.
        /// The output and return values are reference equal.
        /// This method is helps to create a content chain.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="typeName">Name of the existing content type.</param>
        /// <param name="name">Name of the new content.</param>
        /// <returns>The new child content.</returns>
        public static Node CreateChild(this Node parent, string typeName, string name)
        {
            var content = Content.CreateNew(typeName, parent, name);
            content.Save();
            return content.ContentHandler;
        }
    }
}
