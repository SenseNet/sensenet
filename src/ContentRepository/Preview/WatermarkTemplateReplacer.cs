using System;
using System.Collections.Generic;
using System.Web;
using SenseNet.ContentRepository;

namespace SenseNet.Preview
{
    public class WatermarkTemplateReplacer : TemplateReplacerBase
    {
        public override IEnumerable<string> TemplateNames
        {
            get { return new[] { "currentdate", "currenttime", "currentuser", "fullname", "email", "ipaddress" }; }
        }

        public override string EvaluateTemplate(string templateName, string templateExpression, object templatingContext)
        {
            switch (templateName)
            {
                case "currentdate":
                    return DateTime.Today.ToShortDateString();
                case "currenttime":
                    return EvaluateExpression(DateTime.UtcNow, templateExpression, templatingContext);
                case "currentuser":
                    return EvaluateExpression(User.Current as GenericContent, templateExpression, templatingContext);
                case "fullname":
                    return TemplateManager.GetProperty(User.Current as GenericContent, "FullName");
                case "email":
                    return TemplateManager.GetProperty(User.Current as GenericContent, "Email");
                case "ipaddress":
                    return RepositoryTools.GetClientIpAddress();
                default:
                    return base.EvaluateTemplate(templateName, templateExpression, templatingContext);
            }
        }
    }
}
