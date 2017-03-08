using System;
using System.Collections.Generic;
using System.Text;

namespace  SenseNet.ContentRepository.Schema
{
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class RepositoryPropertyAttribute : Attribute
	{
		private string _propertyName;
		private RepositoryDataType _dataType;

		public string PropertyName
		{
			get { return _propertyName; }
			set { _propertyName = value; }
		}
		public RepositoryDataType DataType
		{
			get { return _dataType; }
			set { _dataType = value; }
		}

		public RepositoryPropertyAttribute() : this(null) { }
		public RepositoryPropertyAttribute(string propertyName) : this(propertyName, RepositoryDataType.NotDefined) { }
		public RepositoryPropertyAttribute(RepositoryDataType dataType) : this(null, dataType) { }
		public RepositoryPropertyAttribute(string propertyName, RepositoryDataType dataType)
		{
			_propertyName = propertyName;
			_dataType = dataType;
		}

		public override string ToString()
		{
			string s = String.Concat("[RepositoryProperty(", _propertyName == null ? "null" : String.Concat("\"", _propertyName, "\""));
			s = String.Concat(s, ", DataType.", _dataType, ")]");
			return s;
		}

	}

}