using System;
using System.Collections.Generic;
using System.Globalization;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Search.Parser
{
    [Obsolete("Use ContentQueryTemplateReplacer instead.", false)]
    public class RepositoryLucQueryTemplateReplacer : LucQueryTemplateReplacer
    {
        private static readonly string[] objectNames = new[] { "currentuser", "currentdate", "currentday", "currenttime", "currentmonth" };

        public override IEnumerable<string> ObjectNames
        {
            get { return objectNames; }
        }

        public override string EvaluateObjectProperty(string objectName, string propertyName)
        {
            switch (objectName.ToLower())
            {
                case "currentuser":
                    return GetProperty(User.Current as GenericContent, propertyName);
                case "currentmonth":
                    return string.Format("'{0}-{1}-1'", DateTime.Today.Year, DateTime.Today.Month);
                case "currentdate":
                case "currentday":
                    return string.Format("'{0}'", DateTime.Today.ToString("yyyy-MM-dd"));
                case "currenttime":
                    return string.Format("'{0}'", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                default:
                    return string.Empty;
            }
        }

        protected string GetProperty(GenericContent content, string propertyName)
        {
            if (content == null)
                return string.Empty;
            if (string.IsNullOrEmpty(propertyName))
                return content.Id.ToString();

            var value = content.GetProperty(propertyName);
            return value == null ? string.Empty : value.ToString();
        }

        protected string GetProperty(Node node, string propertyName)
        {
            if (node == null)
                return string.Empty;
            if (string.IsNullOrEmpty(propertyName))
                return node.Id.ToString();

            var value = node[propertyName];
            return value == null ? string.Empty : value.ToString();
        }
    }
}
