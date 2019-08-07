using SenseNet.ApplicationModel;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using SenseNet.Configuration;
using SenseNet.Tools;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Base class that provides methods to generate and validate content names.
    /// </summary>
    public abstract class ContentNamingProvider
    {
        /* instance handling */

        private static ContentNamingProvider __instance;
        private static ContentNamingProvider Instance { get { return __instance; } }

        static ContentNamingProvider()
        {
            var className = Providers.ContentNamingProviderClassName;
            
            ContentNamingProvider instance = null;

            if (string.IsNullOrEmpty(className))
            {
                instance = new CharReplacementContentNamingProvider();
            }
            else
            {
                try
                {
                    instance = (ContentNamingProvider)TypeResolver.CreateInstance(className);
                }
                catch (Exception)
                {
                    SnLog.WriteWarning("Error loading ContentNamingProvider type: " + className, EventId.RepositoryLifecycle);
                }
            }

            if (instance == null)
                instance = new CharReplacementContentNamingProvider();

            SnLog.WriteInformation("ContentNamingProvider created: " + instance);

            __instance = instance;
        }

        /* ContentNamingProvider facade */

        /// <summary>
        /// Provides name for the brand new content.
        /// </summary>
        /// <param name="nameBase">Name without required extension.</param>
        /// <param name="contentType">ContentType that specifies the required name extension.</param>
        /// <param name="parent">Content that will be the parent of the new content. It can help in customization.</param>
        /// <returns></returns>
        public static string GetNewName(string nameBase, ContentType contentType, Node parent)
        {
            return Instance.GenerateNewName(nameBase, contentType, parent);
        }

        /// <summary>
        /// OData function that converts the human readable name to the valid content name.
        /// </summary>
        /// <param name="content">Required parameter for the OData function.</param>
        /// <param name="displayName">Source of the conversion.</param>
        /// <returns>The converted name.</returns>
        [ODataFunction]
        public static string GetNameFromDisplayName(Content content, string displayName)
        {
            return GetNameFromDisplayName(displayName);
        }
        /// <summary>
        /// Converts the human readable name to the valid content name.
        /// </summary>
        /// <param name="displayName">Source of the conversion.</param>
        /// <returns>The converted name.</returns>
        public static string GetNameFromDisplayName(string displayName)
        {
            return GetNameFromDisplayName((string)null, displayName);
        }
        /// <summary>
        /// Converts the human readable name to the valid content name.
        /// </summary>
        /// <param name="originalName">Original name to help conversion.</param>
        /// <param name="displayName">Source of the conversion.</param>
        /// <returns>The converted name.</returns>
        public static string GetNameFromDisplayName(string originalName, string displayName)
        {
            return Instance.GenerateNameFromDisplayName(originalName, displayName);
        }

        /// <summary>
        /// Validates the name. Called when the content is saving.
        /// </summary>
        public static void ValidateName(string name)
        {
            Instance.AssertNameIsValid(name);
        }

        /// <summary>
        /// Helper method that returns with the extension of the name if there is.
        /// Extension is the part of the name after the last period '.' character without the period.
        /// </summary>
        public static string GetFileExtension(string fileName)
        {
            var extension = string.Empty;
            if (fileName != null)
            {
                var index = fileName.LastIndexOf('.');
                if (index != -1 && fileName.Length > index + 1)
                    extension = fileName.Substring(index);
            }
            return extension;
        }

        /// <summary>
        /// Increments the counter suffix in the name e.g. MainDocument(5) -> MainDocument(6).
        /// </summary>
        public static string IncrementNameSuffix(string currentName)
        {
            return Instance.GetNextNameSuffix(currentName);
        }
        /// <summary>
        /// Increments the counter suffix in the name e.g. MainDocument(5) -> MainDocument(6).
        /// Parent id helps to explore the last existing suffix.
        /// </summary>
        public static string IncrementNameSuffixToLastName(string currentName, int parentNodeId)
        {
            return Instance.GetNextNameSuffix(currentName, parentNodeId);
        }
        /// <summary>
        /// Recognizes the existing suffix and separates to the name and a counter.
        /// Counter is 0 if the suffix does not exist.
        /// </summary>
        /// <param name="name">Input name with suffix.</param>
        /// <param name="nameBase">Output parameter that contains the name without suffix.</param>
        /// <returns>Recognized suffix value or 0.</returns>
        public static int ParseSuffix(string name, out string nameBase)
        {
            return Instance.GetNameBaseAndSuffix(name, out nameBase);
        }

        /* Customizable interface */

        /// <summary>
        /// The method of an inherited class converts the human readable name to the valid content name.
        /// </summary>
        /// <param name="originalName">Original name to help conversion.</param>
        /// <param name="displayName">Source of the conversion.</param>
        /// <returns>The converted name.</returns>
        protected abstract string GenerateNameFromDisplayName(string originalName, string displayName);

        /// <summary>
        /// Default implementation of the checking name during saving a content.
        /// Checks whether the given name does not match with the configured InvalidNameCharsPattern.
        /// If the pattern matches, an InvalidPathException will be thrown.
        /// </summary>
        protected virtual void AssertNameIsValid(string name)
        {
            var pattern = ContentNaming.InvalidNameCharsPattern;
            if (!String.IsNullOrEmpty(pattern))
            {
                var match = Regex.Match(name, pattern);
                if (match.Length != 0)
                    throw RepositoryPath.GetInvalidPathException(RepositoryPath.PathResult.InvalidNameChar, name);
            }
        }

        /// <summary>
        /// Default implementation of the name generation for the brand new content.
        /// Enforces the required extension that can differentiate the content subtypes.
        /// </summary>
        /// <param name="nameBase">Name without required extension.</param>
        /// <param name="contentType">ContentType that specifies the required name extension.</param>
        /// <param name="parent">Not used in this implementation.</param>
        /// <returns></returns>
        protected virtual string GenerateNewName(string nameBase, ContentType contentType, Node parent)
        {
            var namewithext = EnforceRequiredExtension(nameBase, contentType);
            return namewithext;
        }
        private string EnforceRequiredExtension(string nameBase, ContentType type)
        {
            if (type != null)
            {
                string reqext = type.Extension;
                if (!string.IsNullOrEmpty(reqext))
                    nameBase = EnsureExtension(nameBase, reqext);
            }
            return nameBase;
        }
        private string EnsureExtension(string nameBase, string reqext)
        {
            var ext = System.IO.Path.GetExtension(nameBase);
            if (string.Equals(ext, reqext))
                return nameBase;
            return nameBase + reqext;
        }

        /// <summary>
        /// Recognizes the existing suffix (an integer in parentheses) and separates to the name and a counter.
        /// Counter is 0 if the suffix does not exist.
        /// </summary>
        /// <param name="name">Input name with suffix.</param>
        /// <param name="nameBase">Output parameter that contains the name without suffix.</param>
        /// <returns>Recognized suffix value or 0.</returns>
        protected virtual int GetNameBaseAndSuffix(string name, out string nameBase)
        {
            nameBase = name;
            if (!name.EndsWith(")"))
                return 0;
            var p = name.LastIndexOf("(");
            if (p < 0)
                return 0;
            var n = name.Substring(p + 1, name.Length - p - 2);
            int result;
            if (Int32.TryParse(n, out result))
            {
                nameBase = name.Substring(0, p);
                return result;
            }
            return 0;
        }
        /// <summary>
        /// Returns with incremented counter suffix in the name e.g. MainDocument(5) -> MainDocument(6).
        /// If the suffix does not exist it will be '(0)'.
        /// Parent id helps to explore the last existing suffix.
        /// </summary>
        protected virtual string GetNextNameSuffix(string currentName, int parentNodeId = 0)
        {
            if (parentNodeId == 0)
            {
                var name = RepositoryPath.GetFileName(currentName);
                var ext = Path.GetExtension(name);
                var fileName = Path.GetFileNameWithoutExtension(name);

                string nameBase;
                var index = ParseSuffix(fileName, out nameBase);

                if (index < 0)
                    index = 0;
                return String.Format("{0}({1}){2}", nameBase, ++index, ext);
            }
            else
            {
                currentName = RepositoryPath.GetFileName(currentName);
                var ext = Path.GetExtension(currentName);
                var fileName = Path.GetFileNameWithoutExtension(currentName);
                var count = ParseSuffix(fileName, out string nameBase);

                var lastName = DataStore.GetNameOfLastNodeWithNameBaseAsync(parentNodeId, nameBase, ext, CancellationToken.None).Result;

                // if there is no suffixed name in db, return with first variant
                if (lastName == null)
                    return $"{nameBase}(1){ext}";

                // there was a suffixed name in db in the form namebase(x), increment it
                // car(5)-> car(6), car(test)(5) -> car(test)(6), car(test) -> car(guid)
                return IncrementNameSuffix(lastName);
            }
        }
    }

    /// <summary>
    /// Contains a method that converts the human readable name to a valid content name.
    /// The names that are generated from unique names are guaranteed to remain unique.
    /// After the conversion the name can contain more characters than the input name.
    /// </summary>
    public class Underscore5FContentNamingProvider : ContentNamingProvider
    {
        private static IDictionary<char, string> _characterCodes = new Dictionary<char, string>() { { '%', "25" }, { '_', "5f" }, { '*', "2a" }, { '\'', "27" }, { '~', "7e" }, { '-', "2d" } };
        private static readonly string ADDITIONALINVALIDCHARS_REGEX = "[%\\*'~]";
        private static char _placeholderSymbol = '_';
        private static string _placeholderSymbolAsString;
        private static string _placeholderSymbolEscaped;

        static Underscore5FContentNamingProvider()
        {
            _placeholderSymbolAsString = _placeholderSymbol.ToString();

            var escaped = HttpUtility.UrlEncode(_placeholderSymbolAsString).Replace('%', _placeholderSymbol);

            // If UrlEncode did not encode the placeholder symbol and we know
            // the code of that character, than use that.
            _placeholderSymbolEscaped = string.Compare(escaped, _placeholderSymbolAsString) == 0 && _characterCodes.ContainsKey(_placeholderSymbol)
                ? _placeholderSymbolAsString + _characterCodes[_placeholderSymbol]
                : escaped;
        }

        /// <summary>
        /// Encodes the name with an UrlEncode like way but the percent sign will be replaced to underscore ('_') character.
        /// The original extension in the 'originalName' parameter will be kept.
        /// </summary>
        /// <param name="originalName">Original name to help conversion.</param>
        /// <param name="displayName">Source of the conversion.</param>
        /// <returns>Replaced name.</returns>
        protected override string GenerateNameFromDisplayName(string originalName, string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return displayName;

            // note: original extension is kept!

            // get original extension
            var origext = GetFileExtension(originalName);

            // get input extension
            var newext = GetFileExtension(displayName);

            // cut extension if there is
            var displayNameWithoutExtension = string.IsNullOrEmpty(newext)
                ? displayName
                : displayName.Substring(0, displayName.Length - newext.Length);

            // convert the displayname to name: if extensions are equal, only filename is to be transformed. extension is added at the end
            var nameToConvert = origext == newext ? displayNameWithoutExtension : displayName;

            string newName = RemoveInvalidCharacters(nameToConvert);

            // attach original extension
            newName = string.Concat(newName, origext);

            return newName.TrimEnd('.', _placeholderSymbol);
        }
        private string RemoveInvalidCharacters(string s)
        {
            // NOTE: changing this code requires a change in RemoveInvalidCharacters, GetNoAccents javascript functions in SN.ContentName.js, in order that these work in accordance

            // replace the escape character with its escaped form
            var validName = Regex.Replace(s, "[" + _placeholderSymbol + "]", _placeholderSymbolEscaped);

            // enforce valid chars pattern
            validName = Regex.Replace(validName, ContentNaming.InvalidNameCharsPattern, delegate(Match match)
            {
                return HttpUtility.UrlEncode(match.ToString()).Replace('%', _placeholderSymbol);
            });

            // encode additional invalid characters that are not handled by UrlEncode
            validName = Regex.Replace(validName, ADDITIONALINVALIDCHARS_REGEX, delegate(Match match)
            {
                var invalidChar = match.ToString()[0];

                // get it from the code table, if it contains the character
                return _placeholderSymbolAsString + (_characterCodes.ContainsKey(invalidChar) ? _characterCodes[invalidChar] : string.Empty);
            });

            return validName;
        }
    }

    /// <summary>
    /// Contains a method that converts the human readable name to a valid content name.
    /// The invalid characters will be replaced to one character.
    /// The conversion is very simple but there is a chance of non-unique name creation.
    /// After the conversion the name can contain less characters than the input name.
    /// </summary>
    public class CharReplacementContentNamingProvider : ContentNamingProvider
    {
        private static readonly char replacementChar;
        private static readonly string replacementString;

        static CharReplacementContentNamingProvider()
        {
            replacementChar = ContentNaming.ReplacementChar;
            replacementString = new String(replacementChar, 1);
        }

        /// <summary>
        /// Replaces the invalid name characters with the configured 'InvalidNameCharsPattern' to the 'ReplacementChar' character.
        /// Duplicated 'ReplacementChar' characters are replaced to one character.
        /// The original extension in the 'originalName' parameter will be kept.
        /// </summary>
        /// <param name="originalName">Original name to help conversion.</param>
        /// <param name="displayName">Source of the conversion.</param>
        /// <returns>Replaced name.</returns>
        protected override string GenerateNameFromDisplayName(string originalName, string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return displayName;

            // note: original extension is kept!

            // get original extension
            var origext = GetFileExtension(originalName);

            // get input extension
            var newext = GetFileExtension(displayName);

            // cut extension if there is
            var displayNameWithoutExtension = string.IsNullOrEmpty(newext)
                ? displayName
                : displayName.Substring(0, displayName.Length - newext.Length);

            // convert the displayname to name: if extensions are equal, only filename is to be transformed. extension is added at the end
            var nameToConvert = origext == newext ? displayNameWithoutExtension : displayName;

            string newName = RemoveInvalidCharacters(nameToConvert);

            // attach original extension
            newName = string.Concat(newName, origext);

            return newName.TrimEnd('.', replacementChar);
        }
        private string RemoveInvalidCharacters(string name)
        {
            name = Regex.Replace(name, ContentNaming.InvalidNameCharsPattern, replacementString);

            var doubleReplacement = new string(replacementChar, 2);
            var length = name.Length;
            while (true)
            {
                name = name.Replace(doubleReplacement, replacementString);
                if (name.Length == length)
                    break;
                length = name.Length;
            }
            return name;
        }
    }

}
