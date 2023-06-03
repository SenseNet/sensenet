using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Common
{
    public static class StringExtensions
    {
        /// <summary>
        /// Truncates this string to the provided maximum length.
        /// </summary>
        /// <param name="value">The original string.</param>
        /// <param name="maxLength">Maximum length of the new string.</param>
        /// <returns>A new string containing the original value truncated to the provided max length.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the provided max length is negative.</exception>
        public static string Truncate(this string value, int maxLength)
        {
            if (maxLength < 0)
                throw new InvalidOperationException("Strings cannot have negative length.");

            return value?.Length <= maxLength ? value : value?.Substring(0, maxLength);
        }
    }
}
