using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Scripting
{
	[global::System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class ScriptTagNameAttribute : Attribute
	{
		public ScriptTagNameAttribute(string scriptTagName)
		{
			this.TagName = scriptTagName;
		}

		public string TagName { get; set; }
	}
}