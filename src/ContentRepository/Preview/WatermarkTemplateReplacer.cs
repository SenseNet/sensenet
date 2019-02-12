using System;
using System.Collections.Generic;
using SenseNet.ContentRepository;

namespace SenseNet.Preview
{
    public class WatermarkTemplateReplacer : TemplateReplacerBase
    {
        public override IEnumerable<string> TemplateNames => new[] { "currentdate", "currenttime", "currentuser", "fullname", "email" };

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
                default:
                    return base.EvaluateTemplate(templateName, templateExpression, templatingContext);
            }
        }
    }
}
