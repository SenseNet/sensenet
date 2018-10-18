using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Sharing;
using SenseNet.Search;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.Search.Indexing
{
    //UNDONE: implement SharingIndexHandler

    /// <summary>
    /// IndexFieldHandler for handling SharingInfo value of a <see cref="Field"/>.
    /// </summary>
    public class SharingIndexHandler : FieldIndexHandler
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;

            if (!(snField is Field field))
                return new IndexField[0];

            if (!(field.Content?.ContentHandler is GenericContent gc))
                return new IndexField[0];

            return GetIndexFields(field.Name, gc.Sharing.Items);
        }

        internal IEnumerable<IndexField> GetIndexFields(string fieldName, IEnumerable<SharingData> sharingItems)
        {
            //UNDONE: Sharing field name constants
            switch (fieldName)
            {
                case "SharedWith":
                    var terms = new List<string>();
                    foreach (var item in sharingItems)
                    {
                        if (!string.IsNullOrEmpty(item.Token))
                            terms.Add(item.Token.ToLowerInvariant());
                        terms.Add(item.Identity.ToString());
                    }
                    var result = CreateField("SharedWith", terms.Distinct().ToArray());
                    return result;
                case "SharedBy":
                    return CreateField("SharedBy", sharingItems
                        .Select(si => si.CreatorId.ToString())
                        .ToArray());
                case "SharingMode":
                    return CreateField("SharingMode", sharingItems
                        .Select(si => si.Mode.ToString().ToLowerInvariant())
                        .ToArray());
                case "SharingLevel":
                    return CreateField("SharingLevel", sharingItems
                        .Select(si => si.Level.ToString().ToLowerInvariant())
                        .ToArray());
            }

            throw new NotImplementedException($"Unknown sharing field: {fieldName}");
        }

        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue)
                ? new IndexValue(dateTimeValue)
                : new IndexValue(text.ToLowerInvariant());
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            //UNDONE: do we need GetParsableValues implementation?
            throw new NotImplementedException();
        }
    }
}
