using System;
using System.Collections.Generic;
using System.Text;

namespace  SenseNet.ContentRepository.Schema
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class ContentHandlerAttribute : Attribute
	{
		public ContentHandlerAttribute() { }
	}
}