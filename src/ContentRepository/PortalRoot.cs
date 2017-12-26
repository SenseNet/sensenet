using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using  SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Represents the root Content of the sensenet Content Repsitory tree.
    /// </summary>
	[ContentHandler]
	public class PortalRoot : Folder
	{
	    /// <inheritdoc />
	    protected PortalRoot(NodeToken nt) : base(nt) { }
	}
}