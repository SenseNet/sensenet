﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Search.Indexing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Events;
using SenseNet.Diagnostics;
using SenseNet.Tools;
using EventId = SenseNet.Diagnostics.EventId;

// ReSharper disable once CheckNamespace
namespace SenseNet.Search
{
    /// <summary>
    /// Content handler class of the IndexingSettings.
    /// </summary>
    [ContentHandler]
    public class IndexingSettings : Settings
    {
        // ================================================================================= Constructors

        /// <summary>
        /// Initializes a new instance of the IndexingSettings.
        /// </summary>
        /// <param name="parent">Existing parent <see cref="Node"/> of the new instance.</param>
        public IndexingSettings(Node parent) : this(parent, null) { }
        /// <summary>
        /// Initializes a new instance of the IndexingSettings.
        /// </summary>
        /// <param name="parent">Existing parent <see cref="Node"/> of the new instance.</param>
        /// <param name="nodeTypeName">TypeName of the inherited class if it has not it own content handler class.</param>
        public IndexingSettings(Node parent, string nodeTypeName) : base(parent, nodeTypeName) {}
        /// <summary>
        /// Initializes a new instance of the IndexingSettings.
        /// This constructor is used for instantiate the saved instance.
        /// </summary>
        protected IndexingSettings(NodeToken nt) : base(nt) { }

        // ================================================================================= Properties

        private const string TextExtractorsTextfieldName = "TextExtractors";
        internal const string TextExtractorsPropertyName = "TextExtractorInstances";
        internal const string SettingsName = "Indexing";

        private static readonly object ExtractorLock = new object();
        private ReadOnlyDictionary<string, ITextExtractor> _textExtractors;
        /// <summary>
        /// Gets the dictionary of the known text extractors.
        /// </summary>
        public ReadOnlyDictionary<string, ITextExtractor> TextExtractorInstances
        {
            get
            {
                if (_textExtractors == null)
                {
                    lock (ExtractorLock)
                    {
                        if (_textExtractors == null)
                        {
                            _textExtractors = new ReadOnlyDictionary<string, ITextExtractor>(LoadTextExtractors());

                            SetCachedData(TextExtractorsCacheKey, _textExtractors);

                            var logger = Providers.Instance.Services.GetRequiredService<ILogger<IndexingSettings>>();
                            logger.LogInformation($"Text extractors were created. File extensions and types: " +
                                $"{string.Join(", ", _textExtractors.Select(t => $"'.{t.Key}': {t.Value.GetType().FullName}"))}");
                        }
                    }
                }

                return _textExtractors;
            }
        }

        /// <summary>
        /// This method collects all the available text extractors. First it builds a list of the built-in
        /// extractors than adds the dynamic ones from the setting (they can override the built-in ones).
        /// </summary>
        private Dictionary<string, ITextExtractor> LoadTextExtractors()
        {
            var instances = Providers.Instance.Services.GetServices<ITextExtractor>().Distinct().ToArray();
            var instancesByType = new Dictionary<Type, ITextExtractor>();
            // only one instance is relevant
            foreach (var instance in instances)
                instancesByType[instance.GetType()] = instance;

            var registration = Providers.Instance.Services.GetServices<TextExtractorRegistration>();
            var extractors = new Dictionary<string, ITextExtractor>();
            // only the last item is relevant
            foreach (var registrationItem in registration)
                extractors[registrationItem.FileExtension] = instancesByType[registrationItem.TextExtractorType];

            // load text extractor settings (they may override the defaults listed above)
            foreach (var field in Content.Fields.Values.Where(field => field.Name.StartsWith(TextExtractorsTextfieldName + ".")))
            {
                var extractorName = field.GetData() as string;
                if (string.IsNullOrEmpty(extractorName))
                    continue;

                extractorName = extractorName.Trim('.', ' ');

                try
                {
                    var extension = field.Name.Substring(field.Name.LastIndexOf('.')).Trim('.', ' ').ToLower();
                    extractors[extension] = (ITextExtractor)TypeResolver.CreateInstance(extractorName);
                }
                catch (Exception ex)
                {
                    SnLog.WriteWarning($"Text extractor type could not be instantiated: {extractorName} {ex}", EventId.Indexing);
                }
            }

            return extractors;
        }

        // ================================================================================= Overrides

        /// <summary>
        /// Returns a value of the property by the given name.
        /// </summary>
        public override object GetProperty(string name)
        {
            switch (name)
            {
                case TextExtractorsPropertyName:
                    return TextExtractorInstances;
                default:
                    return base.GetProperty(name);
            }
        }

        /// <summary>
        /// Sets the value of the property by the given name and object.
        /// </summary>
        public override void SetProperty(string name, object value)
        {
            switch (name)
            {
                case TextExtractorsPropertyName:
                    // this is a readonly property
                    break;
                default:
                    base.SetProperty(name, value);
                    break;
            }
        }

        // ================================================================================= Cached data

        private const string TextExtractorsCacheKey = "CachedTextExtractors";

        /// <summary>
        /// Restores the cached data if there is.
        /// </summary>
        protected override void OnLoaded(object sender, NodeEventArgs e)
        {
            base.OnLoaded(sender, e);

            _textExtractors = (ReadOnlyDictionary<string, ITextExtractor>)GetCachedData(TextExtractorsCacheKey);
        }
    }
}
