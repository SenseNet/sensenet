using System;
using System.Collections.Generic;
using System.Text;

namespace  SenseNet.ContentRepository.Schema
{
	[global::System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class ShortNameAttribute : Attribute
	{
		private string _shortName;

		public string ShortName
		{
			get { return _shortName; }
		}

		public ShortNameAttribute(string shortName)
		{
			_shortName = shortName;
		}
	}
}