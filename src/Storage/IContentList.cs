using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Schema;

namespace SenseNet.ContentRepository.Storage
{
	public interface IContentList
	{
		ContentListType GetContentListType();
	}
}