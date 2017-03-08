using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Runtime.CompilerServices;

namespace SenseNet.ContentRepository.Storage.Schema
{
	public class TypeCollection<T> : IEnumerable<T> where T : SchemaItem
	{
		private ISchemaRoot _schemaRoot;
		private Dictionary<string, T> _list;

		internal TypeCollection(ISchemaRoot schemaRoot)
		{
			if (schemaRoot == null)
				throw new ArgumentNullException("schemaRoot");
			_schemaRoot = schemaRoot;
			_list = new Dictionary<string, T>();
		}
		internal TypeCollection(TypeCollection<T> initialList) : this(initialList._schemaRoot)
		{
			AddRange(initialList);
		}

		public int Count
		{
			get { return _list.Count; }
		}
		public T this[string name]
		{
			get
			{
				T value = default(T);
				_list.TryGetValue(name, out value);
				return value; 
			}
			internal set
			{
				_list[name] = StructureCheck(value);
				if (name != value.Name)
					throw new InvalidSchemaException(SR.Exceptions.Schema.Msg_KeyAndTypeNameAreNotEqual);
			}
		}

		public T this[int index]
		{
			get { return GetValueArray()[index]; }
			internal set { _list[GetNameArray()[index]] = StructureCheck(value); }
		}
		public bool Contains(T item)
		{
			return _list.ContainsValue(item);
		}
		public int IndexOf(T item)
		{
			T[] values = GetValueArray();
			for (int i = 0; i < values.Length; i++)
				if (values[i].Equals(item))
					return i;
			return -1;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_list.Values.CopyTo(array, arrayIndex);
		}
		public T[] CopyTo(int arrayIndex)
		{
			T[] values = new T[_list.Count + arrayIndex];
			_list.Values.CopyTo(values, arrayIndex);
			return values;
		}
		public T[] ToArray()
		{
			T[] result = new T[_list.Values.Count];
			_list.Values.CopyTo(result, 0);
			return result;
		}
		public int[] ToIdArray()
		{
			List<int> _idList = new List<int>(_list.Count);
			foreach (T item in _list.Values)
				_idList.Add(item.Id);
			return _idList.ToArray();
		}
		public string[] ToNameArray()
		{
			string[] names = new string[_list.Count];
			_list.Keys.CopyTo(names, 0);
			return names;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _list.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _list.Values.GetEnumerator();
		}

		public T GetItemById(int itemId)
		{
			foreach (T t in _list.Values)
				if (t.Id == itemId)
					return t;
			return default(T);
		}

		internal virtual void Add(T item)
		{
			_list.Add(item.Name, StructureCheck(item));
		}
		internal virtual void AddRange(TypeCollection<T> items)
		{
			foreach (T t in items)
				if(!_list.ContainsValue(t))
					_list.Add(t.Name, t);
		}
		internal void Insert(int index, T item)
		{
			List<string> keys = new List<string>(_list.Keys);
			List<T> values = new List<T>(_list.Values);
			keys.Insert(index, item.Name);
			values.Insert(index, StructureCheck(item));
			_list.Clear();
			for (int i = 0; i < keys.Count; i++)
				_list.Add(keys[i], values[i]);
		}
		internal void Clear()
		{
			_list.Clear();
		}
		internal bool Remove(T item)
		{
			if (!_list.ContainsValue(item))
				return false;
			_list.Remove(item.Name);
			return true;
		}
		internal void RemoveAt(int index)
		{
			_list.Remove(this[index].Name);
		}

		private T[] GetValueArray()
		{
			T[] values = new T[_list.Count];
			_list.Values.CopyTo(values, 0);
			return values;
		}
		private string[] GetNameArray()
		{
			string[] keys = new string[_list.Count];
			_list.Keys.CopyTo(keys, 0);
			return keys;
		}

		private T StructureCheck(T item)
		{
			if (_schemaRoot != item.SchemaRoot)
				throw new InvalidSchemaException(SR.Exceptions.Schema.Msg_InconsistentHierarchy);
			return item;
		}

	}
}