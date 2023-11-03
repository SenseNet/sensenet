using System;
using System.Collections.Generic;
using System.Linq;
using SNCS = SenseNet.ContentRepository.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.Packaging.Steps;

[Annotation("Switches on or off categories.")]
internal class EditCategories : EditContentType
{
    [DefaultProperty]
    [Annotation("Comma separated words as categories to be added to allowed list")]
    public string Add { get; set; }

    [Annotation("Comma separated words as categories to be removed to allowed list")]
    public string Remove { get; set; }

    public override void Execute(ExecutionContext context)
    {
        ContentType = GetNormalizedStringValue(ContentType, context);
        var newCategories = GetNormalizedStringValue(Add, context);
        var oldCategories = GetNormalizedStringValue(Remove, context);

        if (ContentType == null)
            throw new SnNotSupportedException("Path and ContentType cannot be empty together.");

        if (string.IsNullOrEmpty(newCategories) && string.IsNullOrEmpty(oldCategories))
        {
            Logger.LogMessage(@"There is no any modificaton.");
            return;
        }

        context.AssertRepositoryStarted();

        ExecuteOnContentType(ContentType.Trim(), newCategories, oldCategories);
    }

    private string GetNormalizedStringValue(string value, ExecutionContext context)
    {
        value = context.ResolveVariable(value) as string;
        return value?.Length == 0 ? null : value;
    }

    private void ExecuteOnContentType(string contentTypeName, string newTypes, string oldTypes)
    {
        Logger.LogMessage("Edit ContentType: {0}", contentTypeName);

        var ct = SNCS.ContentType.GetByName(contentTypeName);
        var newChiltTypeNames = string.Join(",", GetEditedList(ct.AllowedChildTypeNames, newTypes, oldTypes));

        var xDoc = LoadContentTypeXmlDocument();

        var insertBefore = LoadChild(xDoc.DocumentElement, "IsSystemType")
                ?? LoadChild(xDoc.DocumentElement, "IsIndexingEnabled")
                ?? LoadChild(xDoc.DocumentElement, "Fields");
        InsertBefore = insertBefore?.LocalName;

        var propertyElement = LoadOrAddChild(xDoc.DocumentElement, "Categories");
        propertyElement.InnerXml = newChiltTypeNames;

        SNCS.ContentTypeInstaller.InstallContentType(xDoc.OuterXml);
    }

    internal static string[] GetEditedList(IEnumerable<string> origItems, string newItems, string retiredItems)
    {
        var origList = origItems?.ToArray() ?? Array.Empty<string>();

        var addArray = newItems?
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray() ?? Array.Empty<string>();
        var removeArray = retiredItems?
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray() ?? Array.Empty<string>();
        var result = origList
            .Union(addArray)
            .Distinct()
            .Except(removeArray)
            .ToArray();

        Logger.LogMessage("Old items: {0}", string.Join(",", origList));
        Logger.LogMessage("New items: {0}", string.Join(",", result));

        return result;
    }
}