using System;
using System.Collections.Generic;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Sharing;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

// ReSharper disable once CheckNamespace
namespace SenseNet.Search.Indexing
{
    internal class SharingDataTokenizer
    {
        public static SharingDataTokenizer Tokenize(SharingData data)
        {
            return new SharingDataTokenizer(data);
        }
        internal static string TokenizeSharingToken(string token)
        {
            return "T" + token.ToLowerInvariant();
        }
        internal static string TokenizeIdentity(int identity)
        {
            return "I" + identity.ToString(); //.ToString("X");
        }
        internal static string TokenizeCreatorId(int creatorId)
        {
            return "C" + creatorId.ToString(); //.ToString("X");
        }
        internal static string TokenizeSharingMode(SharingMode mode)
        {
            return "M" + (int)mode;
        }
        internal static string TokenizeSharingLevel(SharingLevel level)
        {
            return "L" + (int)level;
        }

        public string Token { get; }
        public string Identity { get; }
        public string CreatorId { get; }
        public string Mode { get; }
        public string Level { get; }

        private SharingDataTokenizer(SharingData data)
        {
            Token = TokenizeSharingToken(data.Token);
            Identity = TokenizeIdentity(data.Identity);
            CreatorId = TokenizeCreatorId(data.CreatorId);
            Mode = TokenizeSharingMode(data.Mode);
            Level =TokenizeSharingLevel(data.Level);
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
            //UNDONE:<? implement ConvertToTermValue
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            throw new SnNotSupportedException();
        }
    }
    public class SharedWithIndexHandler : FieldIndexHandler
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;
            return new IndexField[0];
        }
        
        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            // UserId
            if (int.TryParse(text, out int id))
                return new IndexValue(SharingDataTokenizer.TokenizeIdentity(id));

            // User by path
            try
            {
                var user = NodeHead.Get(text);
                if (user != null)
                    return new IndexValue(SharingDataTokenizer.TokenizeIdentity(user.Id));
            }
            catch
            {
                // ignored
            }

            // User by username
            try
            {
                var user = User.Load(text);
                if (user != null)
                    return new IndexValue(SharingDataTokenizer.TokenizeIdentity(user.Id));
            }
            catch
            {
                // ignored
            }

            return new IndexValue(SharingDataTokenizer.TokenizeSharingToken(text));
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            if (value is NodeHead nodeHead)
                return new IndexValue(nodeHead.Id);
            if (value is Node node)
                return new IndexValue(node.Id);
            if (value is int id)
                return new IndexValue(id);
            if (value is string text)
            {
                // User by path
                try
                {
                    var head = NodeHead.Get(text);
                    if (head != null)
                        return new IndexValue(head.Id);
                }
                catch
                {
                    // ignored
                }

                // User by username
                try
                {
                    var userByName = User.Load(text);
                    if (userByName != null)
                        return new IndexValue(userByName.Id);
                }
                catch
                {
                    // ignored
                }

                return new IndexValue(text);
            }
            throw new ApplicationException("Cannot parse the 'SharedBy' expression.");
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            throw new SnNotSupportedException();
        }
    }
    public class SharedByIndexHandler : FieldIndexHandler
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;
            return new IndexField[0];
        }

        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            // UserId
            if (int.TryParse(text, out int id))
                return new IndexValue(SharingDataTokenizer.TokenizeCreatorId(id));

            // User by path
            try
            {
                var user = NodeHead.Get(text);
                if (user != null)
                    return new IndexValue(SharingDataTokenizer.TokenizeCreatorId(user.Id));
            }
            catch
            {
                // ignored
            }

            // User by username
            try
            {
                var user = User.Load(text);
                if (user != null)
                    return new IndexValue(SharingDataTokenizer.TokenizeCreatorId(user.Id));
            }
            catch
            {
                // ignored
            }

            return new IndexValue(text);
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            if (value is NodeHead nodeHead)
                return new IndexValue(nodeHead.Id);
            if (value is Node node)
                return new IndexValue(node.Id);
            if (value is int id)
                return new IndexValue(id);
            if (value is string text)
            {
                // User by path
                try
                {
                    var head = NodeHead.Get(text);
                    if (head != null)
                        return new IndexValue(head.Id);
                }
                catch
                {
                    // ignored
                }

                // User by username
                try
                {
                    var userByName = User.Load(text);
                    if (userByName != null)
                        return new IndexValue(userByName.Id);
                }
                catch
                {
                    // ignored
                }
            }
            throw new ApplicationException("Cannot parse the 'SharedWith' expression.");
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            throw new SnNotSupportedException();
        }
    }
    public class SharingModeIndexHandler : FieldIndexHandler
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;
            return new IndexField[0];
        }

        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return Enum.TryParse(text, true, out SharingMode mode)
                ? new IndexValue(SharingDataTokenizer.TokenizeSharingMode(mode))
                : new IndexValue(text);
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            if(value is string stringValue)
                return new IndexValue(stringValue);

            if (value is SharingMode mode)
                return new IndexValue(mode.ToString());

            if (value is int intValue)
            {
                try
                {
                    var name = Enum.GetName(typeof(SharingMode), intValue);
                    return new IndexValue(name);
                }
                catch (Exception)
                {
                    throw new ApplicationException("Invalid value in the 'SharingMode' expression: " + intValue);
                }
            }

            throw new ApplicationException("Cannot parse the 'SharingMode' expression.");
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            throw new SnNotSupportedException();
        }
    }
    public class SharingLevelIndexHandler : FieldIndexHandler
    {
        /// <inheritdoc />
        public override IEnumerable<IndexField> GetIndexFields(IIndexableField snField, out string textExtract)
        {
            textExtract = string.Empty;
            return new IndexField[0];
        }

        /// <inheritdoc />
        public override IndexValue Parse(string text)
        {
            return Enum.TryParse(text, true, out SharingLevel level)
                ? new IndexValue(SharingDataTokenizer.TokenizeSharingLevel(level))
                : new IndexValue(text);
        }
        /// <inheritdoc />
        public override IndexValue ConvertToTermValue(object value)
        {
            if (value is string stringValue)
                return new IndexValue(stringValue);

            if (value is SharingLevel level)
                return new IndexValue(level.ToString());

            if (value is int intValue)
            {
                try
                {
                    var name = Enum.GetName(typeof(SharingLevel), intValue);
                    return new IndexValue(name);
                }
                catch (Exception)
                {
                    throw new ApplicationException("Invalid value in the 'SharingLevel' expression: " + intValue);
                }
            }

            throw new ApplicationException("Cannot parse the 'SharingLevel' expression.");
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetParsableValues(IIndexableField snField)
        {
            throw new SnNotSupportedException();
        }
    }
}
