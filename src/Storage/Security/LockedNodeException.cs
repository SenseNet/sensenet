using System;

namespace SenseNet.ContentRepository.Storage.Security
{
	public class LockedNodeException : Exception
	{
		private LockHandler _lockHandler;

		public LockHandler LockHandler
		{
			get
			{
				return _lockHandler;
			}
			set
			{
				_lockHandler = value;
			}
		}

		public LockedNodeException(LockHandler lockHandler)
			: base(String.Concat(lockHandler.Node.Path, ": is locked."))
		{
			this._lockHandler = lockHandler;
		}

		public LockedNodeException(LockHandler lockHandler, string message)
			: base(message)
		{
			this._lockHandler = lockHandler;
		}
	}
}