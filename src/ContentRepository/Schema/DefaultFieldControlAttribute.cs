using System;
using System.Collections.Generic;
using System.Text;

namespace  SenseNet.ContentRepository.Schema
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class DefaultFieldControlAttribute : Attribute
	{
		private string _fieldControlTypeName;

		public string FieldControlTypeName
		{
			get { return _fieldControlTypeName; }
			set { _fieldControlTypeName = value; }
		}

		public DefaultFieldControlAttribute(string fieldControlTypeName)
		{
			_fieldControlTypeName = fieldControlTypeName;
		}
	}
}