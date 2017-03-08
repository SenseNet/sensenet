using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage.Events
{
    public enum NodeEvent
    {
        Created = 0,
        Modified = 1,
        Deleted = 2,
        DeletedPhysically = 3,
        Moved = 4,
        Copied = 5,
        PermissionChanged = 6,
        Loaded = 7
    }
    public enum CancellableNodeEvent
    {
        Creating = 10,
        Modifying = 11,
        Deleting = 12,
        DeletingPhysically = 13,
        Moving = 14,
        Copying = 15,
        PermissionChanging = 16
    }
}