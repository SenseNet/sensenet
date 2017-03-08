using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using  SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository
{
	[ContentHandler]
	public class PortalRoot : Folder
	{
		protected PortalRoot(NodeToken nt) : base(nt) { }
	}
}