using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Search;
using SenseNet.Diagnostics;

namespace SenseNet.Search.Indexing
{
    public class IndexDocumentProvider : IIndexDocumentProvider
    {
        private static List<string> SkippedMultistepFields = new List<string>(new[] { "Size" });

        public IndexDocument GetIndexDocument(Node node, bool skipBinaries, bool isNew, out bool hasBinary)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            hasBinary = false;

            if(!StorageContext.Search.ContentRepository.IsContentTypeIndexed(node.NodeType.Name))
                return IndexDocument.NotIndexedDocument;

            var textEtract = new StringBuilder();

            var doc = new IndexDocument {HasCustomField = node is IHasCustomIndexField}; //UNDONE: Unit test IHasCustomIndexField feature
            var ixnode = node as IIndexableDocument;
            var faultedFieldNames = new List<string>();

            if (ixnode == null)
            {
                doc.Add(new IndexField(IndexFieldName.NodeId, node.Id, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));
                doc.Add(new IndexField(IndexFieldName.VersionId, node.VersionId, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));
                doc.Add(new IndexField(IndexFieldName.Version, node.Version.ToString(), IndexingMode.Analyzed, IndexStoringMode.Yes, IndexTermVector.No));
                doc.Add(new IndexField(IndexFieldName.OwnerId, node.OwnerId, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));
                doc.Add(new IndexField(IndexFieldName.CreatedById, node.CreatedById, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));
                doc.Add(new IndexField(IndexFieldName.ModifiedById, node.ModifiedById, IndexingMode.AnalyzedNoNorms, IndexStoringMode.Yes, IndexTermVector.No));
            }
            else
            {
                foreach (var field in ixnode.GetIndexableFields())
                {
                    if (IndexDocument.ForbiddenFields.Contains(field.Name))
                        continue;
                    if (IndexDocument.PostponedFields.Contains(field.Name))
                        continue;
                    if (node.SavingState != ContentSavingState.Finalized && (field.IsBinaryField || SkippedMultistepFields.Contains(field.Name)))
                        continue;
                    if (skipBinaries && (field.IsBinaryField))
                    {
                        if (!StorageContext.Search.ContentRepository.TextExtractingWillBePotentiallySlow(field))
                        {
                            hasBinary = true;
                            continue;
                        }
                    }

                    IEnumerable<IndexField> indexFields = null;
                    string extract = null;
                    try
                    {
                        indexFields = field.GetIndexFields(out extract);
                    }
                    catch (Exception)
                    {
                        faultedFieldNames.Add(field.Name);
                    }

                    if (!String.IsNullOrEmpty(extract)) // do not add extra line if extract is empty
                    {
                        try
                        {
                            textEtract.AppendLine(extract);
                        }
                        catch (OutOfMemoryException)
                        {
                            SnLog.WriteWarning("Out of memory error during indexing.",
                                EventId.Indexing,
                                properties: new Dictionary<string, object>
                                    {
                                        { "Path", node.Path },
                                        { "Field", field.Name }
                                    });
                        }
                    }

                    if (indexFields != null)
                        foreach (var indexField in indexFields)
                            doc.Add(indexField);
                }
            }

            var isInherited = true;
            if (!isNew)
                isInherited = node.IsInherited;
            doc.Add(new IndexField(IndexFieldName.IsInherited, isInherited, IndexingMode.Analyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            doc.Add(new IndexField(IndexFieldName.IsMajor, node.Version.IsMajor, IndexingMode.Analyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            doc.Add(new IndexField(IndexFieldName.IsPublic, node.Version.Status == VersionStatus.Approved, IndexingMode.Analyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            doc.Add(new IndexField(IndexFieldName.AllText, textEtract.ToString(), IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default));

            if (faultedFieldNames.Any())
            {
                doc.Add(new IndexField(IndexFieldName.IsFaulted, true, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
                foreach (var faultedFieldName in faultedFieldNames)
                    doc.Add(new IndexField(IndexFieldName.FaultedFieldName, faultedFieldName, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            }

            // Validation
            if (!doc.HasField(IndexFieldName.NodeId))
                throw new InvalidOperationException("Invalid empty field value for field: " + IndexFieldName.NodeId);
            if (!doc.HasField(IndexFieldName.VersionId))
                throw new InvalidOperationException("Invalid empty field value for field: " + IndexFieldName.VersionId);

            return doc;
        }

        public IndexDocument CompleteIndexDocument(Node node, IndexDocument baseDocument)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var textEtract = new StringBuilder(baseDocument.GetStringValue(IndexFieldName.AllText));

            var faultedFieldNames = new List<string>();
            var ixnode = node as IIndexableDocument;
            if (ixnode != null)
            {
                foreach (var field in ixnode.GetIndexableFields())
                {
                    if (IndexDocument.ForbiddenFields.Contains(field.Name))
                        continue;
                    if (IndexDocument.PostponedFields.Contains(field.Name))
                        continue;
                    if (node.SavingState != ContentSavingState.Finalized && (field.IsBinaryField || SkippedMultistepFields.Contains(field.Name)))
                        continue;
                    if (!field.IsBinaryField)
                        continue;
                    if (!StorageContext.Search.ContentRepository.TextExtractingWillBePotentiallySlow(field))
                        continue;

                    IEnumerable<IndexField> indexFields = null;
                    string extract = null;
                    try
                    {
                        indexFields = field.GetIndexFields(out extract);
                    }
                    catch (Exception)
                    {
                        faultedFieldNames.Add(field.Name);
                    }

                    if (!String.IsNullOrEmpty(extract)) // do not add extra line if extract is empty
                    {
                        try
                        {
                            textEtract.AppendLine(extract);
                        }
                        catch (OutOfMemoryException)
                        {
                            SnLog.WriteWarning("Out of memory error during indexing.",
                                EventId.Indexing,
                                properties: new Dictionary<string, object>
                                {
                                    { "Path", node.Path },
                                    { "Field", field.Name }
                                });
                        }
                    }

                    if (indexFields != null)
                        foreach (var indexField in indexFields)
                            baseDocument.Add(indexField);
                }
            }

            baseDocument.Add(new IndexField(IndexFieldName.AllText, textEtract.ToString(), IndexingMode.Analyzed, IndexStoringMode.No, IndexTermVector.Default));

            if (faultedFieldNames.Any())
            {
                if(!baseDocument.GetBooleanValue(IndexFieldName.IsFaulted))
                    baseDocument.Add(new IndexField(IndexFieldName.IsFaulted, true, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
                foreach (var faultedFieldName in faultedFieldNames)
                    baseDocument.Add(new IndexField(IndexFieldName.FaultedFieldName, faultedFieldName, IndexingMode.NotAnalyzed, IndexStoringMode.Yes, IndexTermVector.Default));
            }

            return baseDocument;
        }
    }
}
