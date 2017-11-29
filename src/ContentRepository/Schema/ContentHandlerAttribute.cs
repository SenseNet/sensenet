using System;
using System.Collections.Generic;
using System.Text;

namespace  SenseNet.ContentRepository.Schema
{
    /// <summary>
    /// Indicates that a class handles a specific type of content in the sensenet content repository.
    /// This class cannot be inherited.
    /// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class ContentHandlerAttribute : Attribute
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentHandlerAttribute"/>.
        /// </summary>
		public ContentHandlerAttribute() { }
	}
}