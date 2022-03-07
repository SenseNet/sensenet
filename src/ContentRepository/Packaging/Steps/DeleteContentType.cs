﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Xml;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Fields;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Search;
using Task = System.Threading.Tasks.Task;

// ReSharper disable PossibleNullReferenceException

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps
{
    public class DeleteContentType : Step
    {
        public enum Mode { No, IfNotUsed, Force }

        internal class ContentTypeDependencies
        {
            public string[] ContentTypeNames { get; set; }
            public string[] InheritedTypeNames { get; set; } = new string[0];
            public int InstanceCount { get; set; }
            public ContentType[] PermittingContentTypes { get; set; } = new ContentType[0];
            public ReferenceFieldSetting[] PermittingFieldSettings { get; set; } = new ReferenceFieldSetting[0];
            public Dictionary<string, string> PermittingContentCollection { get; set; } = new Dictionary<string, string>();
            public string[] ContentTemplates { get; set; } = new string[0];
            public string[] ContentViews { get; set; } = new string[0];
            public string[] Applications { get; set; } = new string[0];

            public bool HasDependency => InheritedTypeNames.Length > 0 || InstanceCount > 0;
        }

        internal class RelatedSensitiveFolders
        {
            public string[] Applications { get; set; }
            public string[] ContentTemplates { get; set; }
            public string[] ContentViews { get; set; }
        }

        [DefaultProperty]
        [Annotation("Name of the content type that will be deleted.")]
        public string Name { get; set; }
        [Annotation("Execution mode.")]
        public Mode Delete { get; set; }

        public override void Execute(ExecutionContext context)
        {
            var names = ResolveVariable(Name, context);

            context.AssertRepositoryStarted();
            using (new SystemAccount())
            {
                var rootTypeNames = GetRootTypeNames(names);
                var rootTypeNamesText = string.Join(", ", rootTypeNames);
                var plural = rootTypeNames.Length > 1;

                if (!rootTypeNames.Any())
                {
                    Logger.LogMessage("There is no content type to delete.");
                    return;
                }

                var dependencies = GetDependencies(rootTypeNames);

                Logger.LogMessage($"DELETING CONTENT TYPE{(plural ? "S" : "")}: " + rootTypeNamesText);

                PrintDependencies(dependencies);

                if (Delete == Mode.No)
                {
                    Logger.LogMessage($"The content type{(plural ? "s are" : " is")} not removed, this step provides only information.");
                    return;
                }

                if (Delete == Mode.IfNotUsed)
                {
                    if (dependencies.HasDependency)
                    {
                        var deps = dependencies.InheritedTypeNames.Length + dependencies.InstanceCount;
                        Logger.LogMessage($"The content type{(plural ? "s were" : " was")} not removed because it have {deps} dependenc{(deps < 2 ? "y" : "ies")}.");
                        return;
                    }
                    Logger.LogMessage($"The content type{(plural ? "s have" : " has")} no any dependencies.");
                }

                if (dependencies.InstanceCount > 0)
                    DeleteInstances(rootTypeNames);
                DeleteRelatedItems(dependencies);
                RemoveAllowedTypes(dependencies);

                foreach (var rootTypeName in rootTypeNames)
                    ContentTypeInstaller.RemoveContentType(rootTypeName);

                Logger.LogMessage($"The content type{(plural ? "s are" : " is")} successfuly removed.");
            }
        }
        private string[] GetRootTypeNames(string input)
        {
            var contentTypePaths = input
                .Trim()
                .Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(ContentType.GetByName)
                .Where(t => t != null)
                .Select(t => t.Path)
                .ToList();

            var rootNames = contentTypePaths
                .Where(p => !contentTypePaths.Any(q => p.StartsWith(q + "/")))
                .Select(RepositoryPath.GetFileName)
                .ToArray();

            return rootNames;
        }

        internal ContentTypeDependencies GetDependencies(string[] rootTypeNames)
        {
            var typeNames = new List<string>();
            var inheritedTypeNames = new List<string>();
            foreach (var rootTypeName in rootTypeNames)
            {
                var typeSubtreeQuery = ContentQuery.CreateQuery(ContentRepository.SafeQueries.InTree,
                    QuerySettings.AdminSettings, ContentType.GetByName(rootTypeName).Path);
                var typeSubtreeResult = typeSubtreeQuery.Execute();
                typeNames.AddRange(typeSubtreeResult.Nodes.Select(n => n.Name));
                inheritedTypeNames.AddRange(typeNames.Except(rootTypeNames));
            }

            var relatedFolders = GetRelatedFolders(typeNames);

            var contentInstancesCount = ContentQuery.CreateQuery(
                                            ContentRepository.SafeQueries.TypeIsCountOnly,
                                            QuerySettings.AdminSettings, new object[] { rootTypeNames }).Execute().Count
                                        -
                                        relatedFolders.ContentTemplates.Select(p => ContentQuery.Query(
                                            ContentRepository.SafeQueries.InTreeAndTypeIsCountOnly,
                                            QuerySettings.AdminSettings, p, typeNames).Count).Sum();

            var PDP = Providers.Instance.GetProvider<IPackagingDataProvider>();
            var result = new ContentTypeDependencies
            {
                ContentTypeNames = rootTypeNames,
                // +InTree:[currentContentType.Path]
                InheritedTypeNames = inheritedTypeNames.ToArray(),
                // +TypeIs:[name]
                InstanceCount = contentInstancesCount,
                // ContentType/AllowedChildTypes: "Folder,File"
                PermittingContentTypes = GetContentTypesWhereTheyAreAllowed(typeNames),
                // ContentType/Fields/Field/Configuration/AllowedTypes/Type: "Folder"
                PermittingFieldSettings = GetContentTypesWhereTheyAreAllowedInReferenceField(typeNames),
                // ContentMetaData/Fields/AllowedChildTypes: "Folder File"
                PermittingContentCollection = PDP.GetContentPathsWhereTheyAreAllowedChildren(typeNames),

                Applications = relatedFolders.Applications,
                ContentTemplates = relatedFolders.ContentTemplates,
                ContentViews = relatedFolders.ContentViews
            };
            return result;
        }

        private ContentType[] GetContentTypesWhereTheyAreAllowed(List<string> names)
        {
            return ContentType.GetContentTypes()
                    .Where(c => !names.Contains(c.Name))
                    .Where(c => c.AllowedChildTypeNames != null && c.AllowedChildTypeNames.Intersect(names).Any())
                    .ToArray();
        }
        private ReferenceFieldSetting[] GetContentTypesWhereTheyAreAllowedInReferenceField(List<string> names)
        {
            return ContentType.GetContentTypes().SelectMany(t => t.FieldSettings)
                .Where(f => f is ReferenceFieldSetting).Cast<ReferenceFieldSetting>()
                .Where(r => r.AllowedTypes != null && r.AllowedTypes.Intersect(names).Any())
                .Distinct()
                .ToArray();
        }

        private RelatedSensitiveFolders GetRelatedFolders(List<string> names)
        {
            var result = ContentQuery.Query(ContentRepository.SafeQueries.TypeIsAndName, QuerySettings.AdminSettings,
                "Folder", names);

            var apps = new List<string>();
            var temps = new List<string>();
            var views = new List<string>();
            foreach (var node in result.Nodes)
            {
                var path = node.Path.ToLowerInvariant();
                if (path.Contains("/(apps)/"))
                    apps.Add(path);
                else if (path.Contains("/contenttemplates/"))
                    temps.Add(path);
                else if (path.Contains("/contentviews/"))
                    views.Add(path);
            }

            return new RelatedSensitiveFolders
            {
                Applications = apps.ToArray(),
                ContentTemplates = temps.ToArray(),
                ContentViews = views.ToArray()
            };
        }

        private void PrintDependencies(ContentTypeDependencies dependencies)
        {
            var names = dependencies.InheritedTypeNames.ToList();
            names.AddRange(dependencies.ContentTypeNames);

            var inheritedTypeNames = dependencies.InheritedTypeNames;
            Logger.LogMessage("  Dependencies: ");
            Logger.LogMessage("    Inherited types: ");
            Logger.LogMessage("      " + (inheritedTypeNames.Length == 0 ? "There is no related item." : string.Join(", ", inheritedTypeNames)));
            Logger.LogMessage(string.Empty);
            Logger.LogMessage("    Content instances: " + dependencies.InstanceCount);
            Logger.LogMessage(string.Empty);

            Logger.LogMessage("  Relations: ");
            if (dependencies.Applications.Length > 0)
            {
                Logger.LogMessage("    Applications: ");
                foreach (var path in dependencies.Applications)
                    Logger.LogMessage($"      {path}");
                Logger.LogMessage(string.Empty);
            }
            if (dependencies.ContentTemplates.Length > 0)
            {
                Logger.LogMessage("    Content templates: ");
                foreach (var path in dependencies.ContentTemplates)
                    Logger.LogMessage($"      {path}");
                Logger.LogMessage(string.Empty);
            }
            if (dependencies.ContentViews.Length > 0)
            {
                Logger.LogMessage("    Content views: ");
                foreach (var path in dependencies.ContentViews)
                    Logger.LogMessage($"      {path}");
                Logger.LogMessage(string.Empty);
            }
            if (dependencies.PermittingContentTypes.Length > 0)
            {
                Logger.LogMessage("    Remaining allowed child types in other content types after deletion:");
                foreach (var contentType in dependencies.PermittingContentTypes)
                {
                    var remaining = string.Join(", ", contentType.AllowedChildTypeNames.Except(names).ToArray());
                    remaining = remaining.Length == 0 ? "[empty]" : remaining;
                    Logger.LogMessage($"      {contentType.Name}: {remaining}");
                }
                Logger.LogMessage(string.Empty);
            }
            if (dependencies.PermittingFieldSettings.Length > 0)
            {
                Logger.LogMessage("    Remaining allowed child types in reference fields after deletion:");
                foreach (var fieldSetting in dependencies.PermittingFieldSettings)
                {
                    var remaining = string.Join(", ", fieldSetting.AllowedTypes.Except(names).ToArray());
                    remaining = remaining.Length == 0 ? "[empty]" : remaining;
                    Logger.LogMessage($"      {fieldSetting.Owner.Name}.{fieldSetting.Name}: {remaining}");
                }
                Logger.LogMessage(string.Empty);
            }
            if (dependencies.PermittingContentCollection.Count > 0)
            {
                Logger.LogMessage("    Remaining allowed child types in content after deletion:");
                foreach (var item in dependencies.PermittingContentCollection)
                {
                    var remaining = string.Join(", ", item.Value.Split(' ').Except(names).ToArray());
                    Logger.LogMessage($"      {item.Key}: {(remaining.Length == 0 ? "[empty]" : remaining)}");
                }
                Logger.LogMessage(string.Empty);
            }
        }

        private void DeleteInstances(string[] contentTypeNames)
        {
            var result = ContentQuery.CreateQuery(
                    ContentRepository.SafeQueries.TypeIs, QuerySettings.AdminSettings, new object[] { contentTypeNames })
                .Execute();

            Logger.LogMessage($"Deleting {result.Count} content by matching content type{(contentTypeNames.Length > 1 ? "s" : "")}.");

            foreach (var node in result.Nodes)
            {
                Logger.LogMessage($"    {node.Path}");
                node.ForceDelete();
            }
        }
        private void DeleteRelatedItems(ContentTypeDependencies dependencies)
        {
            if (dependencies.Applications.Length > 0)
            {
                Logger.LogMessage("Deleting applications...");
                foreach (var node in dependencies.Applications.Select(Node.LoadNode).Where(n => n != null))
                    node.ForceDelete();
                Logger.LogMessage("Ok.");
            }
            if (dependencies.ContentTemplates.Length > 0)
            {
                Logger.LogMessage("Deleting content templates...");
                foreach (var node in dependencies.ContentTemplates.Select(Node.LoadNode).Where(n => n != null))
                    node.ForceDelete();
                Logger.LogMessage("Ok.");
            }
            if (dependencies.ContentViews.Length > 0)
            {
                Logger.LogMessage("Deleting content views...");
                foreach (var node in dependencies.ContentViews.Select(Node.LoadNode).Where(n => n != null))
                    node.ForceDelete();
                Logger.LogMessage("Ok.");
            }
        }
        private void RemoveAllowedTypes(ContentTypeDependencies dependencies)
        {
            Logger.LogMessage("Remove from allowed types:");

            var names = dependencies.InheritedTypeNames.ToList();
            names.AddRange(dependencies.ContentTypeNames);
            var ns = EditContentType.NamespacePrefix;

            if (dependencies.PermittingContentTypes.Length + dependencies.PermittingFieldSettings.Length > 0)
            {
                foreach (var contentType in ContentType.GetContentTypes())
                {
                    var changed = false;
                    var xml = new XmlDocument();
                    xml.Load(contentType.Binary.GetStream());
                    var nsmgr = EditContentType.GetNamespaceManager(xml);

                    var allowedChildTypesElement =
                        (XmlElement)xml.SelectSingleNode($"/{ns}:ContentType/{ns}:AllowedChildTypes", nsmgr);
                    if (allowedChildTypesElement != null)
                    {
                        var oldList = allowedChildTypesElement.InnerText.Split(new[] { ',' },
                            StringSplitOptions.RemoveEmptyEntries);
                        var newList = oldList.Except(names).ToArray();
                        if (oldList.Length != newList.Length)
                        {
                            changed = true;
                            Logger.LogMessage($"    {contentType.Name}");
                            allowedChildTypesElement.InnerText = string.Join(",", newList);
                        }
                    }

                    foreach (XmlElement refFieldElement in xml.SelectNodes($"//{ns}:Field[@type='Reference']", nsmgr))
                    {
                        var elementsToDelete = new List<XmlElement>();
                        var oldAllowedTypes =
                            refFieldElement.SelectNodes($"{ns}:Configuration/{ns}:AllowedTypes/{ns}:Type", nsmgr);
                        foreach (XmlElement typeElement in oldAllowedTypes)
                            if (names.Contains(typeElement.InnerText))
                                elementsToDelete.Add(typeElement);
                        if (elementsToDelete.Any())
                        {
                            var fieldName = refFieldElement.Attributes["name"].Value;
                            Logger.LogMessage($"    {contentType.Name}.{fieldName}");
                            changed = true;
                        }
                        foreach (var element in elementsToDelete)
                            element.ParentNode.RemoveChild(element);
                    }

                    if (changed)
                    {
                        ContentTypeInstaller.InstallContentType(xml.OuterXml);
                    }
                }
            }

            if (dependencies.PermittingContentCollection.Count > 0)
            {
                foreach (var item in dependencies.PermittingContentCollection)
                {
                    Logger.LogMessage($"    {item.Key}");

                    var content = Content.Load(item.Key);
                    if (content != null)
                    {
                        if (content.ContentHandler is GenericContent gc)
                        {
                            var newList = new List<ContentType>();
                            var oldList = gc.AllowedChildTypes.ToList();
                            foreach (var ct in oldList)
                                if (!names.Contains(ct.Name))
                                    newList.Add(ct);

                            gc.AllowedChildTypes = newList;
                            gc.Save();
                        }
                    }
                }
            }
        }
    }
}