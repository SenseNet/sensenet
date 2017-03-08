using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.Search;
using SenseNet.Tools;

namespace SenseNet.ApplicationModel
{
    public class GenericScenario
    {
        public string Name
        {
            get; internal set;
        }

        public virtual IComparer<ActionBase> GetActionComparer()
        {
            return new ActionComparer();
        }

        public IEnumerable<ActionBase> GetActions(Content context, string backUrl)
        {
            var actions = CollectActions(context, backUrl).ToList();

            FilterWithRequiredPermissions(actions, context);

            var comparer = GetActionComparer();
            if (comparer != null) actions.Sort(comparer);

            return actions;
        }

        protected virtual IEnumerable<ActionBase> CollectActions(Content context, string backUrl)
        {
            // customize action list in derived classes
            return ActionFramework.GetActionsFromContentRepository(context, this.Name, backUrl);
        }

        public virtual void Initialize(Dictionary<string, object> parameters)
        {
            // consume custom parameters in the derived class
        }


        public static IEnumerable<Node> GetNewItemNodes(GenericContent content) 
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var types = content.GetAllowedChildTypes();
            if (types is AllContentTypes)
                return new Node[0];

            return GetNewItemNodes(content, types.ToArray());
        }

        public static IEnumerable<Node> GetNewItemNodes(GenericContent content, ContentType[] contentTypes)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var templatesAndTypes = new List<Node>();

            if (contentTypes != null && contentTypes.Length == 0)
                return templatesAndTypes;

            var notAllTypes = contentTypes != null && contentTypes.Length > 0 &&
                              contentTypes.Length < ContentType.GetContentTypes().Length;

            Node site = null;
            try
            {
                site = Node.GetAncestorOfNodeType(content, "Site");
            }
            catch (InvalidOperationException)
            {
                // the user does not have enough permissions for one of the parents
            }

            Node currentWorkspace = null;
            try
            {
                currentWorkspace = Workspace.GetWorkspaceForNode(content);
            }
            catch (InvalidOperationException)
            {
                // the user does not have enough permissions for one of the parents
            }

            var wsTemplatePath = currentWorkspace == null ? string.Empty : RepositoryPath.Combine(currentWorkspace.Path, Repository.ContentTemplatesFolderName);
            var siteTemplatePath = site == null ? string.Empty : RepositoryPath.Combine(site.Path, Repository.ContentTemplatesFolderName);
            var currentContentTemplatePath = RepositoryPath.Combine(content.Path, Repository.ContentTemplatesFolderName);

            // the query is built on the assumption that all content
            // templates are placed under a "TypeName" folder in a
            // container called "ContentTemplates", meaning their
            // depth equals the depth of the container +2.
            var sbQueryText = new StringBuilder("+(");
            
            // add filter for workspace and site templates
            if (!string.IsNullOrEmpty(wsTemplatePath) && wsTemplatePath.CompareTo(currentContentTemplatePath) != 0)
                sbQueryText.AppendFormat("(InTree:\"{0}\" AND Depth:{1}) OR", wsTemplatePath, RepositoryPath.GetDepth(wsTemplatePath) + 2);
            if (!string.IsNullOrEmpty(siteTemplatePath) && siteTemplatePath.CompareTo(currentContentTemplatePath) != 0)
                sbQueryText.AppendFormat(" (InTree:\"{0}\" AND Depth:{1}) OR", siteTemplatePath, RepositoryPath.GetDepth(siteTemplatePath) + 2);

            // add filter for local and global templates
            sbQueryText.AppendFormat(" (InTree:\"{0}\" AND Depth:{1}) OR", currentContentTemplatePath, RepositoryPath.GetDepth(currentContentTemplatePath) + 2);
            sbQueryText.AppendFormat(" (InTree:\"{0}\" AND Depth:{1}))", RepositoryStructure.ContentTemplateFolderPath, RepositoryPath.GetDepth(RepositoryStructure.ContentTemplateFolderPath) + 2);

            // content type filter
            if (notAllTypes)
                sbQueryText.AppendFormat(" +Type:({0})", string.Join(" ", contentTypes.Select(ct => ct.Name)));

            sbQueryText.Append(" .REVERSESORT:Depth");

            var templateResult = ContentQuery.Query(sbQueryText.ToString(), new QuerySettings { EnableAutofilters = FilterStatus.Disabled, EnableLifespanFilter = FilterStatus.Disabled }).Nodes.ToList();
            var templatesNonGlobal = templateResult.Where(ct => !ct.Path.StartsWith(RepositoryStructure.ContentTemplateFolderPath)).ToList();
            var templatesGlobal = templateResult.Where(ct => ct.Path.StartsWith(RepositoryStructure.ContentTemplateFolderPath)).ToList();

            var addedTemplates = new Dictionary<string, List<string>>();

            // add all local and ws/site level templates
            foreach (var localTemplate in templatesNonGlobal)
            {
                // query correction: if the query returned a type that we do not want, skip it
                if (notAllTypes && !contentTypes.Any(ct => ct.Name.CompareTo(localTemplate.ParentName) == 0))
                    continue;

                AddTemplate(templatesAndTypes, localTemplate, addedTemplates);
            }

            // add global templates
            foreach (var globalTemplate in templatesGlobal)
            {
                // query correction: if the query returned a type that we do not want, skip it
                if (notAllTypes && !contentTypes.Any(ct => ct.Name.CompareTo(globalTemplate.ParentName) == 0))
                    continue;

                AddTemplate(templatesAndTypes, globalTemplate, addedTemplates);
            }

            // add content types without a template
            if (contentTypes != null)
                templatesAndTypes.AddRange(contentTypes.Where(contentType => !addedTemplates.ContainsKey(contentType.Name)));

            return templatesAndTypes;
        }

        private static void AddTemplate(ICollection<Node> templates, Node template, IDictionary<string, List<string>> addedTemplates)
        {
            if (addedTemplates.ContainsKey(template.ParentName))
            {
                if (addedTemplates[template.ParentName].Contains(template.Name))
                    return;

                addedTemplates[template.ParentName].Add(template.Name);
            }
            else
            {
                addedTemplates.Add(template.ParentName, new List<string> { template.Name });
            }

            templates.Add(template);
        }
        
        private static void FilterWithRequiredPermissions(IEnumerable<ActionBase> actions, Content context)
        {
            foreach (var action in actions)
            {
                ActionFramework.CheckRequiredPermissions(action, context);
            }
        }
    }

    public class MyScenarioConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return TypeResolver.CreateInstance(value.ToString());
        }
    } 
}
