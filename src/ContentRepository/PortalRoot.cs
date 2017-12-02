using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;
using  SenseNet.ContentRepository.Schema;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Represents the class for the instance of the sensenet repsitory's root Content.
    /// </summary>
	[ContentHandler]
	public class PortalRoot : Folder
	{
	    /// <inheritdoc />
	    protected PortalRoot(NodeToken nt) : base(nt) { }
	}
}