using System;
using System.Collections.Generic;
using System.Text;
using SenseNet.ContentRepository.Storage.Data;
using SenseNet.ContentRepository.Storage.DataModel;
using SenseNet.Search.Indexing;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

internal class ReplicationContext
{
    public string TypeName { get; set; }
    public bool IsSystemContent { get; set; }

    public int CountMax { get; set; }
    public int CurrentCount { get; set; }

    public DateTime ReplicationStart { get; set; }
    public DateTime Now { get; set; }

    public int TargetId { get; set; }
    public string TargetPath { get; set; }

    public NodeHeadData NodeHeadData { get; set; }
    public VersionData VersionData { get; set; }
    public DynamicPropertyData DynamicData { get; set; }

    public IndexDocumentData IndexDocumentPrototype { get; set; }
    public IndexDocumentData IndexDocument { get; set; }
    public StringBuilder TextExtract { get; set; }
    public List<IFieldGenerator> FieldGenerators { get; set; }

    /* ================================================================== INDEX HANDLING */

    public void SetIndexValue(string fieldName, string value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
        TextExtract.AppendLine(value);
    }
    public void SetIndexValue(string fieldName, string[] value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
        foreach (var item in value)
            TextExtract.AppendLine(item);
    }
    public void SetIndexValue(string fieldName, bool value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, int value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, int[] value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, long value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, float value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, double value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }
    public void SetIndexValue(string fieldName, DateTime value)
    {
        var indexField = IndexDocument.IndexDocument.Fields[fieldName];
        IndexDocument.IndexDocument.Fields[fieldName] = new IndexField(fieldName, value,
            indexField.Mode, indexField.Store, indexField.TermVector);
    }

}