using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ApplicationModel
{
    public class ActionComparer : IComparer<ActionBase>
    {
        public int Compare(ActionBase x, ActionBase y)
        {
            if (x == null || y == null)
                return 0;

            return ActionComparerHelper.CompareByIndexAndName(x, y);
        }
    }

    public class ActionComparerByType : IComparer<ActionBase>
    {
        public int Compare(ActionBase x, ActionBase y)
        {
            if (x == null || y == null)
                return 0;

            var xType = GetContentType(x);
            var yType = GetContentType(y);
            var xIsContainer = IsContainer(xType);
            var yIsContainer = IsContainer(yType);

            if (xIsContainer && !yIsContainer)
                return -1;

            if (!xIsContainer && yIsContainer)
                return 1;

            return ActionComparerHelper.CompareByIndexAndText(x, y);
        }

        private static bool IsContainer(ContentType contentType)
        {
            return contentType != null && contentType.IsInstaceOfOrDerivedFrom("Folder");
        }

        private static ContentType GetContentType(ActionBase action)
        {
            var parameters = action.GetParameters();
            var contentTypeName = parameters.ContainsKey("ContentTypeName")
                                      ? parameters["ContentTypeName"] as string ?? string.Empty
                                      : string.Empty;

            if (string.IsNullOrEmpty(contentTypeName))
                return null;

            var contentType = ContentType.GetByName(contentTypeName);
            if (contentType == null)
            {
                contentTypeName = RepositoryPath.GetFileNameSafe(RepositoryPath.GetParentPath(contentTypeName));
                contentType = ContentType.GetByName(contentTypeName);
            }

            return contentType;
        }
    }

    public class ActionComparerByText : IComparer<ActionBase>
    {
        /// <summary>
        /// Compare actions by text and index
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(ActionBase x, ActionBase y)
        {
            if (x == null || y == null)
                return 0;

            var tc = string.Compare(x.Text ?? string.Empty, y.Text ?? string.Empty);

            return tc != 0 ? tc : x.Index.CompareTo(y.Index);
        }
    }

    public class ActionComparerHelper
    {
        public static int CompareByIndexAndName (ActionBase x, ActionBase y)
        {
            var index = x.Index.CompareTo(y.Index);

            return index != 0
                       ? index
                       : (string.IsNullOrEmpty(x.Name) || string.IsNullOrEmpty(y.Name)
                              ? 0
                              : x.Name.CompareTo(y.Name));
        }

        public static int CompareByIndexAndText(ActionBase x, ActionBase y)
        {
            var index = x.Index.CompareTo(y.Index);

            return index != 0
                       ? index
                       : (string.IsNullOrEmpty(x.Text) || string.IsNullOrEmpty(y.Text)
                              ? 0
                              : x.Text.CompareTo(y.Text));
        }
    }
}
