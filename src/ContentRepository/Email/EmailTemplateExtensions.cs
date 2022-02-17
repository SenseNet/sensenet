using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Email
{
    public static class EmailTemplateExtensions
    {
        public static async Task<(string subject, string template)> LoadEmailTemplateAsync(this IEmailTemplateManager tm, 
            string templateName, Func<IDictionary<string, string>, System.Threading.Tasks.Task> fillProperties = null)
        {
            if (string.IsNullOrEmpty(templateName))
                throw new ArgumentNullException(nameof(templateName));

            var properties = new Dictionary<string, string>();
            if (fillProperties != null)
                await fillProperties(properties).ConfigureAwait(false);

            var template = await tm.GetEmailTemplateAsync(templateName).ConfigureAwait(false);
            if (template == null)
                return (string.Empty, string.Empty);

            var subject = !string.IsNullOrEmpty(template.Subject) ? template.Subject : "A notification from sensenet";
            var body = !string.IsNullOrEmpty(template.Body) ? template.Body  : string.Empty;
            
            if (!properties.Any()) 
                return (subject, body);

            // replace variables
            subject = properties.Aggregate(subject, (current, property) => 
                current.Replace($"{{{property.Key}}}", property.Value));
            body = properties.Aggregate(body, (current, property) => 
                current.Replace($"{{{property.Key}}}", property.Value));

            return (subject, body);
        }
    }
}
