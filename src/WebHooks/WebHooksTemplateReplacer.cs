using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Security.Clients;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.WebHooks
{
    internal class WebHooksTemplateReplacerContext
    {
        public WebHookEventType EventType { get; set; }
        public Node Node { get; set; }
        public WebHookSubscription Subscription { get; set; }
    }

    internal class WebHooksTemplateReplacer : TemplateReplacerBase
    {
        private readonly ClientStoreOptions _clientStoreOptions;

        public WebHooksTemplateReplacer(IOptions<ClientStoreOptions> clientStoreOptions)
        {
            _clientStoreOptions = clientStoreOptions.Value;
        }

        /// <summary>
        /// Gets the array of supported template names.
        /// </summary>
        public override IEnumerable<string> TemplateNames => new[]
        {
            "currentuser", "currentdate", "currentday", "today", "currenttime", "content", "eventname", "repository"
        };

        /// <summary>
        /// Resolve a named template in the given expression.
        /// </summary>
        public override string EvaluateTemplate(string templateName, string templateExpression, object templatingContext)
        {
            var context = templatingContext as WebHooksTemplateReplacerContext;

            switch (templateName)
            {
                case "currentuser":
                    return EvaluateExpression(User.Current as GenericContent, templateExpression, templatingContext);
                case "currenttime":
                    return EvaluateExpression(DateTime.UtcNow, templateExpression, templatingContext, "minutes");
                case "currentdate":
                case "currentday":
                case "today":
                    return EvaluateExpression(DateTime.UtcNow.Date, templateExpression, templatingContext, "days");
                case "content":
                    return EvaluateExpression(context?.Node as GenericContent, templateExpression, templatingContext);
                case "eventname":
                    return context?.EventType.ToString() ?? string.Empty;
                case "repository":
                    return _clientStoreOptions.RepositoryUrl?.RemoveUrlSchema();
                default:
                    return base.EvaluateTemplate(templateName, templateExpression, templatingContext);
            }
        }

        protected override string FormatDateTime(DateTime date)
        {
            return date.ToString("O");
        }
    }
}
