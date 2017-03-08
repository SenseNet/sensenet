using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
	public enum VersionStatus
	{
        Approved = 1,
        Locked = 2,
        Draft = 4,
        Rejected = 8,
        Pending = 16
	}
}