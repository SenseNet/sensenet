using System;
using System.Collections.Generic;
using System.Text;

namespace  SenseNet.ContentRepository.Schema
{
	[global::System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class DefaultFieldSettingAttribute : Attribute
	{
		private Type _fieldSettingType;

		public Type FieldSettingType
		{
			get { return _fieldSettingType; }
		}

		public DefaultFieldSettingAttribute(Type fieldSettingType)
		{
			_fieldSettingType = fieldSettingType;
		}
	}

}