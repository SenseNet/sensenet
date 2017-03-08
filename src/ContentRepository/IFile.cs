using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.ContentRepository
{
	public interface IFile
	{
		BinaryData Binary { get; set;}
        long Size { get; }
        long FullSize { get; }
	}

}