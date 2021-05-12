﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.Search;
using SenseNet.Search.Indexing;

namespace SenseNet.ContentRepository.Search.Indexing
{
    /// <summary>
    /// Provides information about indexing. Provides a method for outer component to extend a Content version with text extract.
    /// </summary>
    public static class IndexingTools
    {
        /// <summary>
        /// Extends a Content version with text extract.
        /// </summary>
        public static void AddTextExtract(int versionId, string textExtract)
        {
            // 1: load indexDocument.
            var docData = SearchManager.LoadIndexDocumentByVersionId(versionId);
            var indexDoc = docData.IndexDocument;

            // 2: original and new text extract concatenation.
            textExtract = (indexDoc.GetStringValue(IndexFieldName.AllText) ?? "") + textExtract;

            indexDoc.Add(new IndexField(IndexFieldName.AllText, textExtract, IndexingMode.Analyzed, IndexStoringMode.No,
                IndexTermVector.No));

            // 3: save indexDocument.
            docData.IndexDocumentChanged();
            Providers.Instance.DataStore.SaveIndexDocumentAsync(versionId, indexDoc, CancellationToken.None).GetAwaiter().GetResult();

            // 4: distributed cache invalidation because of version timestamp.
            Providers.Instance.DataStore.RemoveNodeDataFromCacheByVersionId(versionId);

            // 5: index update.
            var node = Node.LoadNodeByVersionId(versionId);
            if (node != null)
                SearchManager.GetIndexPopulator().RebuildIndexAsync(node, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the name of every field in the system.
        /// </summary>
        /// <param name="includeNonIndexedFields">Whether or not to include non-indexed fields. Default is true.</param>
        /// <returns>A list which contains the name of every field in the system which meets the specificed criteria.</returns>
        public static IEnumerable<string> GetAllFieldNames(bool includeNonIndexedFields = true)
        {
            if (includeNonIndexedFields)
                return ContentTypeManager.Instance.AllFieldNames;

            return ContentTypeManager.Instance.AllFieldNames.Where(x => ContentTypeManager.Instance.IndexingInfo.ContainsKey(x) && ContentTypeManager.Instance.IndexingInfo[x].IsInIndex);
        }

        /// <summary>
        /// Gets detailed indexing information about all fields in the repository.
        /// </summary>
        /// <param name="includeNonIndexedFields">Whether to include non-indexed fields.</param>
        /// <returns>Detailed indexing information about all fields in the repository.</returns>
        public static IEnumerable<ExplicitPerFieldIndexingInfo> GetExplicitPerFieldIndexingInfo(bool includeNonIndexedFields)
        {
            var infoArray = new List<ExplicitPerFieldIndexingInfo>(ContentTypeManager.Instance.ContentTypes.Count * 5);

            foreach (var contentType in ContentTypeManager.Instance.ContentTypes.Values)
            {
                var xml = new System.Xml.XmlDocument();
                var nsmgr = new System.Xml.XmlNamespaceManager(xml.NameTable);
                var fieldCount = 0;

                nsmgr.AddNamespace("x", ContentType.ContentDefinitionXmlNamespace);
                xml.Load(contentType.Binary.GetStream());
                var fieldNodes = xml.SelectNodes("/x:ContentType/x:Fields/x:Field", nsmgr);
                if (fieldNodes != null)
                {
                    foreach (System.Xml.XmlElement fieldElement in fieldNodes)
                    {
                        var typeAttr = fieldElement.Attributes["type"] ?? fieldElement.Attributes["handler"];

                        var info = new ExplicitPerFieldIndexingInfo
                        {
                            ContentTypeName = contentType.Name,
                            ContentTypePath =
                                contentType.Path.Replace(Repository.ContentTypesFolderPath + "/", String.Empty),
                            FieldName = fieldElement.Attributes["name"].Value,
                            FieldType = typeAttr.Value
                        };

                        var fieldTitleElement = fieldElement.SelectSingleNode("x:DisplayName", nsmgr);
                        if (fieldTitleElement != null)
                            info.FieldTitle = fieldTitleElement.InnerText;

                        var fieldDescElement = fieldElement.SelectSingleNode("x:Description", nsmgr);
                        if (fieldDescElement != null)
                            info.FieldDescription = fieldDescElement.InnerText;

                        var hasIndexing = false;
                        var indexingNodes = fieldElement.SelectNodes("x:Indexing/*", nsmgr);
                        if (indexingNodes != null)
                        {
                            foreach (System.Xml.XmlElement element in indexingNodes)
                            {
                                if (!Enum.TryParse(element.InnerText, out IndexFieldAnalyzer analyzer))
                                    analyzer = IndexFieldAnalyzer.Default;
                                hasIndexing = true;
                                switch (element.LocalName)
                                {
                                    case "Analyzer":
                                        info.Analyzer = analyzer;
                                        break;
                                    case "IndexHandler":
                                        info.IndexHandler = element.InnerText.Replace("SenseNet.Search", ".");
                                        break;
                                    case "Mode":
                                        info.IndexingMode = element.InnerText;
                                        break;
                                    case "Store":
                                        info.IndexStoringMode = element.InnerText;
                                        break;
                                    case "TermVector":
                                        info.TermVectorStoringMode = element.InnerText;
                                        break;
                                }
                            }
                        }

                        fieldCount++;

                        if (hasIndexing || includeNonIndexedFields)
                            infoArray.Add(info);
                    }
                }

                // content type without fields
                if (fieldCount == 0 && includeNonIndexedFields)
                {
                    var info = new ExplicitPerFieldIndexingInfo
                    {
                        ContentTypeName = contentType.Name,
                        ContentTypePath = contentType.Path.Replace(Repository.ContentTypesFolderPath + "/", String.Empty),
                    };

                    infoArray.Add(info);
                }
            }

            return infoArray;
        }

        /// <summary>
        /// Gets explicit per-field indexing information collected into a table.
        /// </summary>
        /// <param name="fullTable">Whether or not to include non-indexed fields.</param>
        /// <returns>A table containing detailed indexing information.</returns>
        public static string GetExplicitIndexingInfo(bool fullTable)
        {
            var infoArray = GetExplicitPerFieldIndexingInfo(fullTable);

            var sb = new StringBuilder();
            sb.AppendLine("TypePath\tType\tField\tFieldTitle\tFieldDescription\tFieldType\tMode\tStore\tTVect\tHandler\tAnalyzer");
            foreach (var info in infoArray)
            {
                sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}",
                    info.ContentTypePath,
                    info.ContentTypeName,
                    info.FieldName,
                    info.FieldTitle,
                    info.FieldDescription,
                    info.FieldType,
                    info.IndexingMode,
                    info.IndexStoringMode,
                    info.TermVectorStoringMode,
                    info.IndexHandler,
                    info.Analyzer);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
