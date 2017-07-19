using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.ContentRepository
{
    [Obsolete("", true)]//UNDONE:! Delete unused class
    public class RepositoryQueryTemplateReplacer : NodeQueryTemplateReplacer
    {
        private static string[] objectNames = new string[] { "currentuser" };

        public override IEnumerable<string> ObjectNames
        {
            get { return objectNames; }
        }

        public override string EvaluateObjectProperty(string objectName, string propertyName)
        {
            if(objectName == "currentuser")
                return GetProperty(User.Current, propertyName);
            return null;
        }

        protected string GetProperty(IUser user, string propertyName)
        {
            return GetProperty((GenericContent)user, propertyName);
        }
        protected string GetProperty(GenericContent content, string propertyName)
        {
            if (content == null)
                return string.Empty;
            var value = content.GetProperty(propertyName);
            return value == null ? string.Empty : value.ToString();
        }
        protected string GetProperty(Node node, string propertyName)
        {
            if (node == null)
                return string.Empty;
            var value = node[propertyName];
            return value == null ? string.Empty : value.ToString();
        }
    }
}
