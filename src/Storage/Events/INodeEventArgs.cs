using System;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Storage.Events
{
	public interface INodeEventArgs
	{
		Node SourceNode { get; }
		IUser User { get; }
		DateTime Time { get; }
	}
}