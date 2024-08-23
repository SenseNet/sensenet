using System;
using System.Collections.Generic;
using System.Linq;
using SenseNet.ApplicationModel;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using SenseNet.Security;

namespace SenseNet.OData.Operations
{
    public static class TreeOperations
    {
        /// <summary>
        /// Gets a list of ancestor content items of the target content. The list will also contain child elements along
        /// the way so that a subtree can be built from the list.
        /// </summary>
        /// <snCategory>Content Management</snCategory>
        /// <param name="content"></param>
        /// <param name="request"></param>
        /// <param name="rootPath">A root ancestor content. This is where the ancestor list will start.</param>
        /// <param name="withLeaves">Whether the response should contain child elements that are not folders (default: false).</param>
        /// <param name="withSystem">Whether the response should contain system content (default: false).</param>
        /// <returns>A list of parent and child content items between the provided root and the target content.</returns>
        /// <exception cref="ODataException"></exception>
        /// <exception cref="ContentNotFoundException"></exception>
        [ODataFunction(Category = "Content Management")]
        [ContentTypes(N.CT.GenericContent, N.CT.ContentType)]
        [AllowedRoles(N.R.Everyone)]
        public static object OpenTree(Content content, ODataRequest request, string rootPath,
            bool withLeaves = false, bool withSystem = false)
        {
            if (!content.Path.Equals(rootPath, StringComparison.OrdinalIgnoreCase) &&
                !content.Path.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase))
                throw new ODataException(
                    $"The '{rootPath}' is not an ancestor of the context content: '{content.Path}'.",
                    ODataExceptionCode.NotSpecified);

            var rootContent = LoadContentSafe(rootPath);
            if (rootContent == null)
                throw new ContentNotFoundException(rootPath);

            string sortField;
            if (request.HasSelect)
            {
                var select = request.Select;
                if (!select.Contains("Id"))
                    select.Add("Id");
                if (!select.Contains("ParentId"))
                    select.Add("ParentId");
                if (!select.Contains("IsSystemContent"))
                    select.Add("IsSystemContent");
                sortField = select.Contains("DisplayName") ? "DisplayName" : "Name";
            }
            else
            {
                sortField = "DisplayName";
            }

            var ancestorContents = GetAncestors(content, rootPath);
            var ancestors = ProcessChildren(ancestorContents, sortField, withLeaves, withSystem);

            return ancestors;
        }
        private static List<Content> GetAncestors(Content content, string rootPath)
        {
            var result = new List<Content>();

            for (var c = content; c != null && c.Path.Length >= rootPath.Length; c = LoadParentSafe(c))
                result.Add(c);

            result.Reverse();
            return result;
        }
        private static Content LoadParentSafe(Content content)
        {
            try
            {
                var parentId = content.ContentHandler.ParentId;
                return Content.Load(parentId);
            }
            catch (SenseNetSecurityException)
            {
                return null;
            }
            catch (AccessDeniedException)
            {
                return null;
            }
        }
        private static Content LoadContentSafe(string rootPath)
        {
            try
            {
                return Content.Load(rootPath);
            }
            catch (SenseNetSecurityException)
            {
                // do nothing
            }
            catch (AccessDeniedException)
            {
                // do nothing
            }

            return null;
        }
        private static List<Content> ProcessChildren(List<Content> ancestors, string sortField, bool withLeaves, bool withSystem)
        {
            var result = new List<Content> { ancestors[0] };

            for (int i = 0; i < ancestors.Count; i++)
            {
                var content = ancestors[i];

                // Set sorting and filters
                if (withSystem)
                    content.ChildrenDefinition.EnableAutofilters = FilterStatus.Disabled;
                content.ChildrenDefinition.Sort = new[] { new SortInfo(sortField) };
                if (!withLeaves)
                    content.ChildrenDefinition.ContentQuery = "+TypeIs:(Folder ContentType)";

                var children = content.Children.ToList();

                // add next axis element to the current child level if the autofilter drops it.
                if (ancestors.Count < i + 1 && children.All(x => x.Id != ancestors[i + 1].Id))
                {
                    children.Add(ancestors[i + 1]);
                    children = sortField == "Name"
                        ? children.OrderBy(x => x.Name).ToList()
                        : children.OrderBy(x => x.DisplayName).ToList();
                }

                result.AddRange(children);
            }

            return result;
        }
    }
}
