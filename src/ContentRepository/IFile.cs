using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Defines an interface for handling Content instances with a primary blob.
    /// </summary>
	public interface IFile
	{
	    /// <summary>
	    /// Gets or sets the metadata of the primary blob. The blob's stream can be accessed through this object.
	    /// </summary>
		BinaryData Binary { get; set;}
	    /// <summary>
	    /// Gets the count of bytes of the blob's stream in the Binary property.
	    /// </summary>
        long Size { get; }
        long FullSize { get; } //UNDONE: Make obsolete
    }

}