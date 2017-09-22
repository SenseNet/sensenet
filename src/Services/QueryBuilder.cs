using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Diagnostics;
using SenseNet.Search;
using SenseNet.ContentRepository.Fields;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal
{
    public class QueryBuilder
    {
        public static readonly string QueryContainerName = "Queries";
        public static readonly string QueryTypeName = "Query";

        // helper classes for type and field serialization
        private class type { public string c; public string n; public string d; public field[] f; }                     // c: content type, n: name, d: displayname, f: fields
        private class field { public string c; public string n; public string d; public string t; public option[] q; }  // c: content type, n: name, d: displayname, t: fieldType, q: options if type is choice
        private class option { public string n; public string v; public bool e; public bool s; }                        // n: name, v: value, e: enabled, s: selected

        /// <summary>
        /// Generic OData action method for collecting all fields of all types in the system.
        /// </summary>
        /// <param name="content">Compulsory generic OData action parameter, currently not used.</param>
        /// <returns>Two arrays: one with regular fields and one for aspect fields.</returns>
        [ODataFunction]
        public static string GetMetadata(Content content)
        {
            // collects regular fields
            var types1 = new List<type>();
            var i = 0;
            foreach (var contentType in ContentType.GetContentTypes())
            {
                if (contentType.Name.ToLower().Contains("fieldsetting"))
                    continue;

                var c = "ct" + i;
                var ct = new type { n = contentType.Name, d = SNSR.GetString(contentType.DisplayName), c = c };
                types1.Add(ct);
                var fields = new List<field>();
                var fieldSettings = contentType.FieldSettings.Where(s => s.IndexingInfo.IsInIndex);
                foreach (var fieldSetting in fieldSettings)
                {
                    if (fieldSetting.Name == "FieldSettingContents" || fieldSetting.Name == "AllFieldSettingContents")
                        continue;

                    var displayName = String.IsNullOrEmpty(fieldSetting.DisplayName) ? fieldSetting.Name : SNSR.GetString(fieldSetting.DisplayName);
                    var fieldType = fieldSetting.ShortName;
                    var choiceField = fieldSetting as ChoiceFieldSetting;
                    if (choiceField != null)
                    {
                        var opts = choiceField.Options.Select(o => new option { n = o.Text, v = o.Value, e = o.Enabled, s = o.Selected }).ToArray();
                        fields.Add(new field { n = fieldSetting.Name, d = displayName, t = "choice", c = c, q = opts });
                    }
                    else if (fieldType != "Binary")
                        fields.Add(new field { n = fieldSetting.Name, d = displayName, t = fieldType, c = c });
                }
                ct.f = fields.OrderBy(x => x.d).ToArray();
                i++;
            }

            types1 = types1.OrderBy(x => x.d).ToList();

            // collect aspect fields
            var types2 = new List<type>();
            var aspects = ContentQuery.Query("TypeIs:Aspect .AUTOFILTERS:OFF").Nodes;
            i = 0;

            foreach (Aspect aspect in aspects)
            {
                var c = "a" + i;
                var at = new type { n = aspect.Name, d = SNSR.GetString(aspect.DisplayName), c = c };
                types2.Add(at);
                var fields = new List<field>();
                foreach (var field in aspect.FieldSettings)
                {
                    if (field != null)
                    {
                        var displayName = String.IsNullOrEmpty(field.DisplayName) ? field.Name : SNSR.GetString(field.DisplayName);
                        var fieldType = field.ShortName;
                        if (fieldType != "BinaryData")
                            fields.Add(new field { n = field.Name, d = displayName, t = fieldType, c = c });
                    }
                }
                at.f = fields.OrderBy(x => x.d).ToArray();
                i++;
            }

            types2 = types2.OrderBy(x => x.d).ToList();

            var sb = new StringBuilder();
            var settings = new JsonSerializerSettings { Formatting = Formatting.None, NullValueHandling = NullValueHandling.Ignore };
            var serializer = JsonSerializer.Create(settings);

            using (var writer = new StringWriter(sb))
                serializer.Serialize(writer, new[] { types1, types2 });

            return sb.ToString();
        }

        [ODataFunction]
        public static IEnumerable<Content> GetQueries(Content content, bool onlyPublic = false)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var wsPath = ((GenericContent) content.ContentHandler).WorkspacePath;
            var profilePath = ((User) User.Current).GetProfilePath();
            
            // path filters: workspace (for public queries) and user profile (for private queries)
            if (string.IsNullOrEmpty(wsPath))
            {
                // no workspace found, and get only public queries --> nothing to return
                if (onlyPublic)
                    return new List<Content>();

               return Content.All.DisableAutofilters().Where(c => c.InTree(RepositoryPath.Combine(profilePath, QueryContainerName)) && c.TypeIs(QueryTypeName));
            }
            
            if (onlyPublic)
            {
                // return only public queries, saved under the workspace
                return Content.All.DisableAutofilters().Where(c => c.InTree(RepositoryPath.Combine(wsPath, QueryContainerName)) && c.TypeIs(QueryTypeName));
            }
            
            // return both public and private queries
            return Content.All.DisableAutofilters().Where(c =>
                (c.InTree(RepositoryPath.Combine(profilePath, QueryContainerName)) ||
                 c.InTree(RepositoryPath.Combine(wsPath, QueryContainerName))) && c.TypeIs(QueryTypeName));
        }

        [ODataAction]
        public static object SaveQuery(Content content, string query, string displayName, string queryType)
        {
            var qt = string.IsNullOrEmpty(queryType)
                         ? QueryType.Public
                         : (QueryType) Enum.Parse(typeof (QueryType), queryType);

            return SaveQuery(content, query, displayName, qt);
        }

        /// <summary>
        /// Add or edit a saved content query.
        /// </summary>
        /// <param name="content">A query content to modify, a user, or any content under a workspace.</param>
        /// <param name="query">Query text.</param>
        /// <param name="displayName">Display name for the saved query.</param>
        /// <param name="queryType">Type of the query.</param>
        /// <returns></returns>
        public static object SaveQuery(Content content, string query, string displayName, QueryType queryType)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            if (query == null)
                throw new ArgumentNullException("query");

            try
            {
                // We need to validate the query to avoid saving unknown texts.
                LucQuery.Parse(query);
            }
            catch (Exception ex)
            {
                SnLog.WriteWarning("Content query parse error during query save.", EventId.Querying,
                    properties: new Dictionary<string, object>
                    {
                        {"User", User.Current.Name},
                        {"Query text", query},
                        {"Error", ex.Message}
                    });

                return string.Empty;
            }

            ContentList queryContainer = null;
            Content queryContent = null;

            switch (queryType)
            {
                case QueryType.Private:

                    // load the user and his profile
                    var user = content.ContentHandler as User ?? (User)User.Current;
                    if (!user.IsProfileExist())
                        user.CreateProfile();

                    var profile = user.Profile;
                    if (profile == null)
                        throw new InvalidOperationException("User profile could not be created.");

                    queryContainer = GetQueryContainer(profile);
                    break;

                case QueryType.Public:
                    // store the query under the current workspace
                    queryContainer = GetQueryContainer(((GenericContent)content.ContentHandler).Workspace);
                    if (queryContainer == null)
                        throw new InvalidOperationException("Query container could not be created for a public query. Content: " + content.Path);
                    break;

                case QueryType.NonDefined:
                    if (!content.ContentType.IsInstaceOfOrDerivedFrom(QueryTypeName))
                        throw new InvalidOperationException("If the query type is nondefined, the content must be a query to save.");
                    queryContent = content;
                    break;

                default:
                    throw new InvalidOperationException("Unknown query type: " + queryType);
            }

            if (queryContainer != null)
            {
                // create a new query under the previously found container
                queryContent = Content.CreateNew(QueryTypeName, queryContainer, null);
            }

            if (queryContent == null)
                throw new InvalidOperationException("No query content to save.");

            // Elevation: a simple user does not necessarily have
            // 'Add' permission for the public queries folder.
            using (new SystemAccount())
            {
                if (!string.IsNullOrEmpty(displayName))
                    queryContent.DisplayName = displayName;

                queryContent["Query"] = query;
                queryContent.Save(); 
            }

            return queryContent;
        }

        private static ContentList GetQueryContainer(Node parent)
        {
            if (parent == null)
                return null;

            var qc = Node.Load<ContentList>(RepositoryPath.Combine(parent.Path, QueryContainerName));
            if (qc == null)
            {
                using (new SystemAccount())
                {
                    var c = Content.CreateNew("CustomList", parent, QueryContainerName);
                    
                    // override the property instead of using the Set method to have full control
                    qc = (ContentList)c.ContentHandler;
                    qc.AllowedChildTypes = new[] { ContentType.GetByName(QueryTypeName) };
                    c["Hidden"] = true;

                    c.Save();
                }
            }

            return qc;
        }
    }
}
