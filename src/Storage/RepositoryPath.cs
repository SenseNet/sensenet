using System;
using System.Text;
using System.Text.RegularExpressions;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// This class handles the path and its methods.
    /// </summary>
    public static class RepositoryPath
    {
        public enum PathResult
        {
            Correct = 0, Empty, TooLong, InvalidPathChar, InvalidNameChar, StartsWithSpace, EndsWithSpace, InvalidFirstChar, EndsWithDot
        }

        /// <summary>
        /// Gets the path separator. It is independent of the operating system you use. Its value always "/"
        /// </summary>
        /// <value>The path separator.</value>
        public static string PathSeparator { get; internal set; } = "/";

        /// <summary>
        /// Gets the path separator characters.
        /// </summary>
        /// <value>The path separator characters.</value>
		public static char[] PathSeparatorChars { get; internal set; } = PathSeparator.ToCharArray();

        [Obsolete("After V6.5 PATCH 9: Use ContentNaming.InvalidNameCharsPattern instead.")]
        public static string InvalidNameCharsPattern => ContentNaming.InvalidNameCharsPattern;
        [Obsolete("After V6.5 PATCH 9: Use ContentNaming.ReplacementChar instead.")]
        public static char ReplacementChar => ContentNaming.ReplacementChar;

        [Obsolete("After V6.5 PATCH 9: Use ContentNaming.InvalidNameCharsPatternForClient instead.")]
        public static string InvalidNameCharsPatternForClient => ContentNaming.InvalidNameCharsPatternForClient;

        private static string _invalidPathCharsPattern;
        public static string InvalidPathCharsPattern
        {
            get
            {
                if (_invalidPathCharsPattern == null)
                {
                    // invalid path pattern is invalid name pattern + '/'
                    if (ContentNaming.InvalidNameCharsPattern.StartsWith("[^"))
                    {
                        // allowed chars are given in invalid chars with negate (^) ---> / is allowed in path
                        _invalidPathCharsPattern = ContentNaming.InvalidNameCharsPattern.Contains(PathSeparator)
                            ? ContentNaming.InvalidNameCharsPattern
                            : string.Concat(
                                ContentNaming.InvalidNameCharsPattern.Substring(0,
                                    ContentNaming.InvalidNameCharsPattern.Length - 1), PathSeparator, "]");
                    }
                    else
                    {
                        // invalid chars are given in invalid chars ---> / is not an invalid char in path
                        _invalidPathCharsPattern = ContentNaming.InvalidNameCharsPattern.Contains(PathSeparator)
                            ? ContentNaming.InvalidNameCharsPattern.Replace(PathSeparator, string.Empty)
                            : ContentNaming.InvalidNameCharsPattern;
                    }
                }
                return _invalidPathCharsPattern;
            }
        }

        public static int MaxLength => Data.DataStore.Enabled ? Data.DataStore.PathMaxLength : Data.DataProvider.Current.PathMaxLength; //DB:ok

        // ===================================================================================================== Methods 

        /// <summary>
        /// Gets the parent path from a path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Parent path</returns>
        public static string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            var index = path.LastIndexOf(PathSeparator, StringComparison.Ordinal);
            return index <= 0 ? string.Empty : path.Substring(0, index);
        }

        /// <summary>
        /// Concatenates the specified pathes.
        /// </summary>
        /// <param name="path1">The path 1.</param>
        /// <param name="path2">The path 2.</param>
        /// <returns>The combined path</returns>
		public static string Combine(string path1, string path2)
        {
            if (path1 == null)
                throw new ArgumentNullException("path1");
            if (path2 == null)
                throw new ArgumentNullException("path2");
            if (path1.Length == 0)
                return path2;
            if (path2.Length == 0)
                return path1;

            var x = 0;
            if (path1[path1.Length - 1] == '/')
                x += 2;
            if (path2[0] == '/')
                x += 1;
            switch (x)
            {
                case 0:    //  path1,   path2
                    return String.Concat(path1, "/", path2);
                case 1:    //  path1,  /path2
                case 2:    //  path1/,  path2
                    return String.Concat(path1, path2);
                case 3:    //  path1/, /path2
                    var sb = new StringBuilder(path1);
                    sb.Length--;
                    sb.Append(path2);
                    return sb.ToString();
            }
            return null; // cannot run here
        }

        public static string Combine(params string[] pathList)
        {
            if (pathList == null)
                throw new ArgumentNullException("pathList");

            var length = pathList.Length;
            if (length == 0)
                return string.Empty;

            var index = 1;
            var path = pathList[0];
            while (index < length)
            {
                path = Combine(path, pathList[index]);
                index++;
            }

            return path;
        }

        /// <summary>
        /// Gets the file name from a valid path, which is located after the last PathSeparator.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The file name</returns>
		public static string GetFileName(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            int p = path.LastIndexOf(PathSeparator, StringComparison.Ordinal);
            if (p < 0)
                return path;
            return path.Substring(p + 1);
        }
        //TODO: Missing unit test
        //TODO: Check Substring outbound
        /// <summary>
        /// Gets the file name from a path, which is located after the last PathSeparator, without checking path validity.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The file name</returns>
        public static string GetFileNameSafe(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            var p = path.LastIndexOf(RepositoryPath.PathSeparator, StringComparison.Ordinal);
            return p <= 0 ? path : path.Substring(p + 1);
        }

        /// <summary>
        /// Determines whether the specified path is valid.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        /// 	<c>true</c> if the specified path is valid; otherwise, <c>false</c>.
        /// </returns>
        public static PathResult IsValidPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                return PathResult.Empty;
            if (path.Length > (Data.DataStore.Enabled ? Data.DataStore.PathMaxLength : Data.DataProvider.Current.PathMaxLength)) //DB:ok
                return PathResult.TooLong;
            if (PathContainsInvalidChar(path))
                return PathResult.InvalidPathChar;

            string[] segments = path.Split(PathSeparator.ToCharArray());
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (i == 0)
                {
                    if (segment.Length != 0)
                        return PathResult.InvalidFirstChar;
                }
                else
                {
                    if (segment.Length == 0)
                        return PathResult.Empty;
                    if (Char.IsWhiteSpace(segment[0]))
                        return PathResult.StartsWithSpace;
                    if (Char.IsWhiteSpace(segment[segment.Length - 1]))
                        return PathResult.EndsWithSpace;
                    if (segment[segment.Length - 1] == '.')
                        return PathResult.EndsWithDot;
                }
            }
            return PathResult.Correct;
        }
        public static PathResult IsValidName(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                return PathResult.Empty;
            if (name.Contains(PathSeparator))
                return PathResult.InvalidNameChar;
            if (NameContainsInvalidChar(name))
                return PathResult.InvalidNameChar;
            if (Char.IsWhiteSpace(name[0]))
                return PathResult.StartsWithSpace;
            if (Char.IsWhiteSpace(name[name.Length - 1]))
                return PathResult.EndsWithSpace;
            if (name[name.Length - 1] == '.')
                return PathResult.EndsWithDot;
            return PathResult.Correct;
        }
        public static void CheckValidName(string name)
        {
            PathResult result = IsValidName(name);
            if (result == PathResult.Correct)
                return;
            throw GetInvalidPathException(result, name);
        }
        public static void CheckValidPath(string path)
        {
            PathResult result = IsValidPath(path);
            if (result == PathResult.Correct)
                return;
            throw GetInvalidPathException(result, path);
        }

        public static bool IsInTree(string targetPath, string rootPath)
        {
            if (rootPath.Equals(targetPath, StringComparison.InvariantCultureIgnoreCase))
                return true;
            return targetPath.StartsWith(RepositoryPath.Combine(rootPath, RepositoryPath.PathSeparator), StringComparison.InvariantCultureIgnoreCase);
        }

        public static Exception GetInvalidPathException(PathResult result, string path)
        {
            switch (result)
            {
                default:
                case PathResult.Correct:
                    return null;
                case PathResult.Empty:
                    // Name cannot be empty.
                    return new InvalidPathException(EmptyNameMessage);
                case PathResult.TooLong:
                    // Path too long. Max length is {0}.
                    return new InvalidPathException(string.Format(PathTooLongMessage, Data.DataStore.Enabled ? Data.DataStore.PathMaxLength : Data.DataProvider.Current.PathMaxLength)); //DB:ok
                case PathResult.InvalidPathChar:
                    // Content path may only contain alphanumeric characters or '.', '(', ')', '[', ']', '/'!
                    return new InvalidPathException(String.Concat(InvalidPathMessage, " Path: ", path));
                case PathResult.InvalidNameChar:
                    // Content name may only contain alphanumeric characters or '.', '(', ')', '[', ']'!
                    return new InvalidPathException(String.Concat(InvalidNameMessage, " Path: ", path));
                case PathResult.StartsWithSpace:
                    // Name cannot start with whitespace.
                    return new InvalidPathException(NameStartsWithWhitespaceMessage);
                case PathResult.EndsWithSpace:
                    // Name cannot end with whitespace.
                    return new InvalidPathException(NameEndsWithWhitespaceMessage);
                case PathResult.InvalidFirstChar:
                    // Path must start with '/' character.
                    return new InvalidPathException(PathFirstCharMessage);
                case PathResult.EndsWithDot:
                    // Path cannot end with '.' character.
                    return new InvalidPathException(PathEndsWithDotMessage);
            }
        }

        public static int GetDepth(string path)
        {
            if (string.IsNullOrEmpty(path))
                return 0;

            var depth = path.Split(PathSeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length - 1;
            return depth;
        }

        /* ========================================================================== Messages */

        private static string EmptyNameMessage => SR.GetStringOrDefault(SR.Exceptions.General.Error_EmptyNameMessage,
            "EmptyNameMessage", "Name cannot be empty.");
        private static string PathTooLongMessage => SR.GetStringOrDefault(SR.Exceptions.General.Error_PathTooLong_MaxValue_1,
            "PathTooLongMessage", "Path too long. Max length is {0}.");
        private static string InvalidPathMessage => SR.GetStringOrDefault(SR.Exceptions.General.Error_InvalidPathMessage,
            "InvalidPathMessage", "Content path may only contain alphanumeric characters or '.', '(', ')', '[', ']', '-', '_', ' ', '/' !");
        private static string InvalidNameMessage => SR.GetStringOrDefault(SR.Exceptions.General.Error_InvalidNameMessage,
            "InvalidNameMessage", "Content name may only contain alphanumeric characters or '.', '(', ')', '[', '], '-', '_', ' ' !");
        private static string NameStartsWithWhitespaceMessage => SR.GetStringOrDefault(SR.Exceptions.General.Error_NameStartsWithWhitespaceMessage,
            "NameStartsWithWhitespaceMessage", "Name cannot start with whitespace.");
        private static string NameEndsWithWhitespaceMessage => SR.GetStringOrDefault(SR.Exceptions.General.Error_NameEndsWithWhitespaceMessage,
            "NameEndsWithWhitespaceMessage", "Name cannot end with whitespace.");
        private static string PathFirstCharMessage => SR.GetStringOrDefault(SR.Exceptions.General.Error_PathFirstCharMessage,
            "PathFirstCharMessage", "Path must start with '/' character.");
        private static string PathEndsWithDotMessage => SR.GetStringOrDefault(SR.Exceptions.General.Error_PathEndsWithDotMessage,
            "PathEndsWithDotMessage", "Path cannot end with '.' character.");
        
        /* ========================================================================== Helper methods */
        public static bool IsInvalidNameChar(char c)
        {
            return new Regex(ContentNaming.InvalidNameCharsPattern).IsMatch(c.ToString());
        }
        public static bool IsInvalidPathChar(char c)
        {
            return new Regex(InvalidPathCharsPattern).IsMatch(c.ToString());
        }
        public static bool NameContainsInvalidChar(string s)
        {
            var stripped = new Regex(ContentNaming.InvalidNameCharsPattern).Replace(s, string.Empty);
            return stripped != s;
        }
        public static bool PathContainsInvalidChar(string s)
        {
            var stripped = new Regex(InvalidPathCharsPattern).Replace(s, string.Empty);
            return stripped != s;
        }

    }
}