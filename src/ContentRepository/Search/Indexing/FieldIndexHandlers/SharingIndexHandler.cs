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
    internal class SharingDataTokenizer
    {
        public static SharingDataTokenizer Tokenize(SharingData data)
        {
            return new SharingDataTokenizer(data);
        }

        public string Token { get; }
        public string Identity { get; }
        public string CreatorId { get; }
        public string Mode { get; }
        public string Level { get; }

        private SharingDataTokenizer(SharingData data)
        {
            Token = "T" + data.Token.ToLowerInvariant();
            Identity = "I" + data.Identity.ToString("X");
            CreatorId = "C" + data.CreatorId.ToString("X");
            Mode = "M" + (int)data.Mode;
            Level = "L" + (int)data.Level;
        }

        public string[] GetCombinations()
        {
            var a = Token;
            var b = Identity;
            var c = CreatorId;
            var d = Mode;
            var e = Level;

            return new[]
            {
                a, b, c, d, e,

                $"{a},{b}", $"{a},{c}", $"{a},{d}", $"{a},{e}",
                $"{b},{c}", $"{b},{d}", $"{b},{e}",
                $"{c},{d}", $"{c},{e}",
                $"{d},{e}",

                $"{a},{b},{c}",$"{a},{b},{d}",$"{a},{b},{e}",$"{a},{c},{d}",$"{a},{c},{e}",$"{a},{d},{e}",
                $"{b},{c},{d}",$"{b},{c},{e}",$"{b},{d},{e}",
                $"{c},{d},{e}",

                $"{a},{b},{c},{d}",$"{a},{b},{c},{e}",$"{a},{b},{d},{e}",$"{a},{c},{d},{e}",$"{b},{c},{d},{e}",

                $"{a},{b},{c},{d},{e}"
            };
        }
    }

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

            if (field.Name != "Sharing")
                return new IndexField[0];

            if (!(field.Content?.ContentHandler is GenericContent gc))
                return new IndexField[0];

            return GetIndexFields(field.Name, gc.Sharing.Items);
        }
        internal IEnumerable<IndexField> GetIndexFields(string fieldName, IEnumerable<SharingData> sharingItems)
        {
            var values = new List<string>();
            foreach (var item in sharingItems)
            {
                var tokenizer = SharingDataTokenizer.Tokenize(item);
                values.AddRange(tokenizer.GetCombinations());
            }
            return CreateField(fieldName, values);
        }

        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return new IndexValue(text);
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            //UNDONE: implement ConvertToTermValue
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            //UNDONE: do we need GetParsableValues implementation? If not, convert to NotSupportedException.
            throw new NotImplementedException();
        }
    }
}
