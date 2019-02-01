using System.Collections.Generic;
using SenseNet.ContentRepository;
using SenseNet.Services;

namespace SenseNet.Preview
{
    internal class WatermarkServicesTemplateReplacer : WatermarkTemplateReplacer
    {
        public override IEnumerable<string> TemplateNames => new[] { "ipaddress" };

        public override string EvaluateTemplate(string templateName, string templateExpression, object templatingContext)
        {
            switch (templateName)
            {
                case "ipaddress":
                    return ServiceTools.GetClientIpAddress();
                default:
                    return base.EvaluateTemplate(templateName, templateExpression, templatingContext);
            }
        }
    }
}
