using SenseNet.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SenseNet.ContentRepository.Storage.Security
{
    internal static class SecurityExtensions
    {
        public static void Export(this AceInfo aceInfo, XmlWriter writer)
        {
            writer.WriteStartElement("Identity");
            writer.WriteAttributeString("path", NodeHead.Get(aceInfo.IdentityId).Path);
            if (aceInfo.LocalOnly)
                writer.WriteAttributeString("propagation", "LocalOnly");
            var values = aceInfo.GetPermissionValues();
            foreach (var permType in PermissionType.PermissionTypes)
            {
                var value = values[permType.Index];
                if (value == PermissionValue.Undefined)
                    continue;
                writer.WriteElementString(permType.Name, value.ToString());
            }
            writer.WriteEndElement();
        }
    }
}
