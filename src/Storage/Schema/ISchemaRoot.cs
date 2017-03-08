using System;
using System.Collections.Generic;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Schema
{
	internal interface ISchemaRoot
	{
		TypeCollection<PropertyType> PropertyTypes { get;}
		TypeCollection<NodeType> NodeTypes { get;}
		TypeCollection<ContentListType> ContentListTypes { get;}

		void Clear();
		void Load();
	}
}