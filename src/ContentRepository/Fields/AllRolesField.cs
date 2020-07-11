using System.Linq;
using System.Xml;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository.Fields
{
    [ShortName("AllRoles")]
    [DefaultFieldSetting(typeof(NullFieldSetting))]
    public class AllRolesField : ReferenceField
    {
        public override bool ReadOnly => true;

        protected override void ImportData(XmlNode fieldNode, ImportContext context)
        {
            throw new SnNotSupportedException();
        }

        public override object GetData()
        {
            return SecurityHandler.SecurityContext
                .GetParentGroups(this.Content.Id, false)
                .Select(Node.LoadNode)
                .ToArray();
        }
    }
}
