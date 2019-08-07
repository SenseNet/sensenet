using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using SenseNet.ContentRepository.Schema;
using System.IO;
using System.Threading;
using SenseNet.ContentRepository.Storage;
using System.Web;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Configuration;

namespace SenseNet.ContentRepository
{
    [Obsolete("Use ContentNamingProvider class instead.")]
    public static class ContentNamingHelper
    {
        /* ========================================================================== Consts and properties */

        private static char _placeholderSymbol = '_';
        public static char PlaceholderSymbol
        {
            get
            {
                return _placeholderSymbol;
            }
        }

        // these are the characters that we encode manually
        private static IDictionary<char, string> _characterCodes = new Dictionary<char, string>() { { '%', "25" }, { '_', "5f" }, { '*', "2a" }, { '\'', "27" }, { '~', "7e" }, { '-', "2d" } };
        private static readonly string ADDITIONALINVALIDCHARS_REGEX = "[%\\*'~]";

        private static string _placeholderSymbolEscaped = null;

        /// <summary>
        /// Returns the escaped value of the placeholder character.
        /// </summary>
        private static string PlaceholderSymbolEscaped
        {
            get
            {
                if (_placeholderSymbolEscaped == null)
                {
                    var placeholder = PlaceholderSymbol.ToString();
                    var escaped = HttpUtility.UrlEncode(placeholder).Replace('%', PlaceholderSymbol);

                    // If UrlEncode did not encode the placeholder symbol and we know
                    // the code of that character, than use that.
                    _placeholderSymbolEscaped = string.Compare(escaped, placeholder) == 0 && _characterCodes.ContainsKey(PlaceholderSymbol)
                        ? placeholder + _characterCodes[PlaceholderSymbol]
                        : escaped;
                }

                return _placeholderSymbolEscaped;
            }
        }


        #region Obsolete methods
        [Obsolete("Use other non-obsolete methods of the ContentNamingProvider class.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static bool IsUriClean(string s)
        {
            string sd = s.Normalize(NormalizationForm.FormD);
            for (int i = 0; i < sd.Length; i++)
            {
                if (!CharIsAllowed(sd[i]))
                    return false;
            }
            if (sd[sd.Length - 1] == '.')
                return false;

            return true;
        }

        [Obsolete("Use other non-obsolete methods of the ContentNamingProvider class.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static string UriCleanup(string s)
        {
            var clean = Strip(s);

            if (string.IsNullOrEmpty(clean))
                clean = Guid.NewGuid().ToString();

            clean = TailClean(clean);

            return clean;
        }

        [Obsolete("Use other non-obsolete methods of the ContentNamingProvider class.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private static string TailClean(string s)
        {
            int b, e;
            for (b = 0; b < s.Length && !NonStrippingChar(s[b]); b++) { }
            for (e = s.Length - 1; e >= 0 && !NonStrippingChar(s[e]); e--) { }

            return s.Substring(b, e - b + 1);
        }

        [Obsolete("Use other non-obsolete methods of the ContentNamingProvider class.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private static string Strip(string name)
        {
            int rank = 0;

            string s = name.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            bool lastIsUnderscore = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (CharIsAllowed(s[i]))
                {
                    sb.Append(s[i]);
                    lastIsUnderscore = false;
                    if (Char.IsLetterOrDigit(s[i]))
                        rank++;
                }
                else if (!lastIsUnderscore && Char.GetUnicodeCategory(s[i]) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(PlaceholderSymbol);
                    lastIsUnderscore = true;
                }
            }

            if (rank > 0)
                return (sb.ToString().Normalize(NormalizationForm.FormC));

            return string.Empty;
        }

        [Obsolete("Use other non-obsolete methods of the ContentNamingProvider class.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private static bool NonStrippingChar(char c)
        {
            return (Char.IsLetterOrDigit(c) || StrongSymbols.Contains(c));
        }

        [Obsolete("Use other non-obsolete methods of the ContentNamingProvider class.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private static bool CharIsAllowed(char c)
        {
            return (Char.IsLetterOrDigit(c) || AllowedSymbolsInName.Contains(c));
        }

        [Obsolete("Use other non-obsolete methods of the ContentNamingProvider class.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static char[] AllowedSymbolsInName
        {
            get { return new char[] { '(', ')', '[', ']', '.', '-', '+', '_' }; }
        }

        [Obsolete("Use other non-obsolete methods of the ContentNamingProvider class.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private static char[] StrongSymbols
        {
            get { return new char[] { '(', ')', '[', ']' }; }
        }

        #endregion


        /* ========================================================================== Methods for ensureing contentname and extension*/
        public static string GetNewName(string nameBase, ContentType type, Node parent)
        {
            var namewithext = EnforceRequiredExtension(nameBase, type);
            return namewithext;
        }
        public static string EnforceRequiredExtension(string nameBase, ContentType type)
        {
            if (type != null)
            {
                string reqext = type.Extension;

                if (!string.IsNullOrEmpty(reqext))
                    nameBase = EnsureExtension(nameBase, reqext);
            }

            return nameBase;
        }
        public static string EnsureExtension(string nameBase, string reqext)
        {
            var ext = System.IO.Path.GetExtension(nameBase);
            if (string.Equals(ext, reqext))
                return nameBase;

            return nameBase + reqext;
        }
        private static string GetNewPath(Node container, string defaultName, int index, out string newName)
        {
            var ext = Path.GetExtension(defaultName);
            var fileName = Path.GetFileNameWithoutExtension(defaultName);

            newName = index == 0 ? defaultName : String.Format("{0}({1}){2}", fileName, index, ext);

            return RepositoryPath.Combine(container.Path, newName);
        }
        public static string IncrementNameSuffixToLastName(string currentName, int parentNodeId)
        {
            currentName = RepositoryPath.GetFileName(currentName);
            var ext = Path.GetExtension(currentName);
            var fileName = Path.GetFileNameWithoutExtension(currentName);
            var count = ParseSuffix(fileName, out var nameBase);

            var lastName = DataStore.GetNameOfLastNodeWithNameBaseAsync(parentNodeId, nameBase, ext, CancellationToken.None).Result;

            // if there is no suffixed name in db, return with first variant
            if (lastName == null)
                return $"{nameBase}(1){ext}";

            // there was a suffixed name in db in the form namebase(x), increment it
            // car(5)-> car(6), car(test)(5) -> car(test)(6), car(test) -> car(guid)
            return IncrementNameSuffix(lastName);
        }
        public static string IncrementNameSuffix(string name)
        {
            name = RepositoryPath.GetFileName(name);
            var ext = Path.GetExtension(name);
            var fileName = Path.GetFileNameWithoutExtension(name);

            string nameBase;
            var index = ParseSuffix(fileName, out nameBase);

            if (index < 0)
                index = 0;
            return String.Format("{0}({1}){2}", nameBase, ++index, ext);
        }
        /// <summary>
        /// Parses name from format 'name(x)'
        /// </summary>
        /// <param name="name">name to parse</param>
        /// <param name="nameBase">parsed namebase</param>
        /// <param name="inValidNumber">true if correct format is detected but (x) is not a valid number</param>
        /// <returns>the parsed number in suffix</returns>
        public static int ParseSuffix(string name, out string nameBase)
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


        /* ========================================================================== Methods for converting displayname in sync with name and displayname fieldcontrols */
        /// <summary>
        /// Removes invalid characters from the provided displayname, without enforcing original extension.
        /// </summary>
        /// <param name="displayName">The input displayname that will be converted to a valid url name.</param>
        /// <returns></returns>
        public static string GetNameFromDisplayName(string displayName)
        {
            return GetNameFromDisplayName(null, displayName);
        }
        /// <summary>
        /// Removes invalid characters from the provided displayname, leaving original extension intact.
        /// </summary>
        /// <param name="originalName">The original name of the Content, to keep its original extension. If unknown, provide extension only in the form of '.ext'. If original extension is not to be kept, provide null.</param>
        /// <param name="displayName">The input displayname that will be converted to a valid url name.</param>
        /// <returns></returns>
        public static string GetNameFromDisplayName(string originalName, string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return displayName;

            // note: original extension is kept!

            // get original extension
            var origext = GetFileExtension(originalName);

            // get input extension
            var newext = GetFileExtension(displayName);

            // convert the displayname to name: if extensions are equal, only filename is to be transformed. extension is added at the end
            var nameToConvert = origext == newext ? GetFileNameWithoutExtension(displayName, newext) : displayName;

            string newName = RemoveInvalidCharacters(nameToConvert);

            // attach original extension
            newName = string.Concat(newName, origext);

            return newName.TrimEnd('.', PlaceholderSymbol);
        }
        /// <summary>
        /// Removes invalid characters from the provided displayname.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveInvalidCharacters(string s)
        {
            // NOTE: changing this code requires a change in RemoveInvalidCharacters, GetNoAccents javascript functions in SN.ContentName.js, in order that these work in accordance

            // replace the escape character with its escaped form
            var validName = Regex.Replace(s, "[" + PlaceholderSymbol + "]", PlaceholderSymbolEscaped);

            // enforce valid chars pattern
            validName = Regex.Replace(validName, ContentNaming.InvalidNameCharsPattern, delegate(Match match)
            {
                return HttpUtility.UrlEncode(match.ToString()).Replace('%', PlaceholderSymbol);
            });

            // encode additional invalid characters that are not handled by UrlEncode
            validName = Regex.Replace(validName, ADDITIONALINVALIDCHARS_REGEX, delegate(Match match)
            {
                var invalidChar = match.ToString()[0];

                // get it from the code table, if it contains the character
                return PlaceholderSymbol.ToString() + (_characterCodes.ContainsKey(invalidChar) ? _characterCodes[invalidChar] : string.Empty);
            });

            return validName;
        }
        /// <summary>
        /// Removes accents from input string.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static string GetNoAccents(string r)
        {
            // NOTE: changing this code requires a change in GetValidUrlName, GetNoAccents javascript function in SN.ContentName.js, in order that these work in accordance
            r = new Regex("[àáâãäå]").Replace(r, "a");
            r = new Regex("[ÀÁÂÃÄÅ]").Replace(r, "A");
            r = new Regex("æ").Replace(r, "ae");
            r = new Regex("Æ").Replace(r, "AE");
            r = new Regex("ç").Replace(r, "c");
            r = new Regex("Ç").Replace(r, "C");
            r = new Regex("[èéêë]").Replace(r, "e");
            r = new Regex("[ÈÉÊË]").Replace(r, "E");
            r = new Regex("[ìíîï]").Replace(r, "i");
            r = new Regex("[ÌÍÎÏ]").Replace(r, "I");
            r = new Regex("ñ").Replace(r, "n");
            r = new Regex("Ñ").Replace(r, "N");
            r = new Regex("[òóôõöőø]").Replace(r, "o");
            r = new Regex("[ÒÓÔÕÖŐØ]").Replace(r, "O");
            r = new Regex("œ").Replace(r, "oe");
            r = new Regex("Œ").Replace(r, "OE");
            r = new Regex("ð").Replace(r, "d");
            r = new Regex("Ð").Replace(r, "D");
            r = new Regex("ß").Replace(r, "s");
            r = new Regex("[ùúûüű]").Replace(r, "u");
            r = new Regex("[ÙÚÛÜŰ]").Replace(r, "U");
            r = new Regex("[ýÿ]").Replace(r, "y");
            r = new Regex("[ÝŸ]").Replace(r, "Y");

            return r;
        }
        /// <summary>
        /// Gets the extension from a provided filename. Return string contains the '.' character.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
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
        /// Gets the filename without the provided extension.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string GetFileNameWithoutExtension(string fileName, string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return fileName;

            return fileName.Substring(0, fileName.Length - extension.Length);
        }
    }
}
