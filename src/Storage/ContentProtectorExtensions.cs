using SenseNet.ContentRepository.Storage;
using SenseNet.Tools;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class ContentProtectorExtensions
    {
        /// <summary>
        /// Adds the passed paths and all parent paths to the not-deletable Content whitelist.
        /// </summary>
        /// <param name="builder">The <see cref="IRepositoryBuilder"/> instance.</param>
        /// <param name="paths">Repository paths that will be added to the whitelist.</param>
        /// <returns>The passed <see cref="IRepositoryBuilder"/> instance.</returns>
        public static IRepositoryBuilder ProtectContent(this IRepositoryBuilder builder, params string[] paths)
        {
            ContentProtector.AddPaths(paths);
            return builder;
        }

        /// <summary>
        /// Adds the provided paths to the list of groups to protect.
        /// </summary>
        /// <param name="builder">The <see cref="IRepositoryBuilder"/> instance.</param>
        /// <param name="paths">Group paths that will be added to the list to protect.</param>
        /// <returns>The passed <see cref="IRepositoryBuilder"/> instance.</returns>
        public static IRepositoryBuilder ProtectGroups(this IRepositoryBuilder builder, params string[] paths)
        {
            ContentProtector.AddGroupPaths(paths);
            return builder;
        }
    }
}
