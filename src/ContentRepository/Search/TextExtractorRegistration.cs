using System;
using SenseNet.ContentRepository.Search.Indexing;
// ReSharper disable once CheckNamespace

namespace SenseNet.Search
{
    /// <summary>
    /// Represents a text extractor assignment to a file extension.
    /// </summary>
    public class TextExtractorRegistration
    {
        /// <summary>
        /// Gets or sets the file extension. Do not start with the '.' character.
        /// </summary>
        public string FileExtension { get; set; }
        /// <summary>
        /// Type of the <see cref="ITextExtractor"/> implementation.
        /// </summary>
        public Type TextExtractorType { get; set; }
    }
}
