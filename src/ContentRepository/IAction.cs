using SenseNet.ContentRepository.Storage.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace SenseNet.ContentRepository
{
    public interface IAction
    {
        string Name { get; }
        bool Enabled { get; }
        bool Visible { get; }
        IEnumerable<PermissionType> RequiredPermissions { get; }

        IAction Clone();
    }
}
