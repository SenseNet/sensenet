using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// This class can prevent the executability of the  Content Delete, ForceDelete, Move operations.
    /// Supports an extendable whitelist of the not-deletable Content paths.
    /// </summary>
    public class ContentProtector
    {
        private readonly List<string> _protectedPaths = new List<string>
        {
            "/Root",
            "/Root/IMS",
            "/Root/IMS/BuiltIn",
            "/Root/IMS/BuiltIn/Portal",
            "/Root/IMS/BuiltIn/Portal/Admin",
            "/Root/IMS/BuiltIn/Portal/Administrators",
            "/Root/IMS/BuiltIn/Portal/Visitor",
            "/Root/IMS/BuiltIn/Portal/Everyone",
            "/Root/IMS/Public",
            "/Root/IMS/Public/Administrators",
            "/Root/System",
            "/Root/System/Schema",
            "/Root/System/Schema/ContentTypes",
            "/Root/System/Schema/ContentTypes/GenericContent",
            "/Root/System/Schema/ContentTypes/GenericContent/Folder",
            "/Root/System/Schema/ContentTypes/GenericContent/File",
            "/Root/System/Schema/ContentTypes/GenericContent/User",
            "/Root/System/Schema/ContentTypes/GenericContent/Group",
        };

        private static ContentProtector Instance => Providers.Instance.ContentProtector;

        /// <summary>
        /// Returns the whitelist of all protected paths.
        /// WARNING: The protected paths are sensitive information.
        /// </summary>
        public static string[] GetProtectedPaths()
        {
            return Instance._protectedPaths.ToArray();
        }

        /// <summary>
        /// If the whitelist contains the passed path, an <see cref="ApplicationException"/> will be thrown.
        /// WARNING: The protected paths are sensitive information.
        /// </summary>
        /// <param name="path">The examined path.</param>
        public static void AssertIsDeletable(string path)
        {
            if (Instance._protectedPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                throw new ApplicationException("Protected content cannot be deleted or moved.");
        }

        /// <summary>
        /// Adds the all passed paths and their complete ancestor axis to the whitelist of the not-deletable Contents.
        /// </summary>
        /// <param name="paths">One or pore paths that will be added to.</param>
        public static void AddPaths(params string[] paths)
        {
            IEnumerable<string> GetAncestorAxis(string path)
            {
                var p = path.Length;
                while (true)
                {
                    path = path.Substring(0, p);
                    yield return path;
                    p = path.LastIndexOf("/", StringComparison.Ordinal);
                    if (path.Length <= Identifiers.RootPath.Length)
                        break;
                }
            }

            var allPaths = paths
                .SelectMany(GetAncestorAxis)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToArray();

            var protectedPaths = Instance._protectedPaths;
            protectedPaths.AddRange(allPaths.Except(protectedPaths, StringComparer.OrdinalIgnoreCase));
        }
    }
}
