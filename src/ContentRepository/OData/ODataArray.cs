using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.OData
{
    /// <summary>
    /// Represents an enumerable parameter of the OData Method Based Operation.
    /// The value can be parsed from json array, querystring array and comma separated list.
    /// </summary>
    public class ODataArray<T> : IEnumerable<T>
    {
        public static readonly char DefaultSeparator = ',';
        public static readonly char[] AvailableSeparators = ",;:|".ToCharArray();

        private readonly List<T> _list;

        public ODataArray(IEnumerable<T> collection)
        {
            _list = new List<T>(collection.ToArray());
        }
        public ODataArray(string commaSeparated)
        {
            _list = ParseList(commaSeparated);
        }

        private List<T> ParseList(string commaSeparated)
        {
            if(string.IsNullOrEmpty(commaSeparated))
                return new List<T>();

            char separator;
            string source;
            if (AvailableSeparators.Contains(commaSeparated[0]))
            {
                separator = commaSeparated[0];
                source = commaSeparated.Substring(1);
            }
            else
            {
                separator = DefaultSeparator;
                source = commaSeparated;
            }

            var array = source.Split(separator).Select(x=>x.Trim()).ToArray();
            var t = typeof(T);
            if (t == typeof(string))
                return array.Cast<T>().ToList();
            if (t == typeof(int))
                return array.Select(int.Parse).Cast<T>().ToList();
            if (t == typeof(long))
                return array.Select(long.Parse).Cast<T>().ToList();
            if (t == typeof(byte))
                return array.Select(byte.Parse).Cast<T>().ToList();
            if (t == typeof(bool))
                return array.Select(bool.Parse).Cast<T>().ToList();
            if (t == typeof(decimal))
                return array.Select(x=> decimal.TryParse(x, out var v) ? v : 
                    decimal.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture)).Cast<T>().ToList();
            if (t == typeof(float))
                return array.Select(x => float.TryParse(x, out var v) ? v :
                    float.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture)).Cast<T>().ToList();
            if (t == typeof(double))
                return array.Select(x => double.TryParse(x, out var v) ? v :
                    double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture)).Cast<T>().ToList();
            return array.Select(Parse).ToList();
        }
        public virtual T Parse(string inputValue)
        {
            throw new NotSupportedException($"ODataArray<{typeof(T).Name}> is not supported. To support, override the 'T Parse(string)' method");
        }

        /* ============================================================= IList<T> implementation */

        public int Count => _list.Count;
        public bool IsReadOnly => true;

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }
    }
}
