using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Schema;

namespace SenseNet.Packaging.Steps
{
    public class ChangeContentType : Step
    {
        /// <summary>Content query for collecting content to be changed.</summary>
        [Annotation("Content query for collecting content to be changed.")]
        public string ContentQuery { get; set; }

        /// <summary>Name of the target content type.</summary>
        [Annotation("Name of the target content type.")]
        public string ContentTypeName { get; set; }

        public override void Execute(ExecutionContext context)
        {
            context.AssertRepositoryStarted();

            if (string.IsNullOrEmpty(ContentQuery) || string.IsNullOrEmpty(ContentTypeName))
                throw new PackagingException(SR.Errors.InvalidParameters);

            var ct = ContentType.GetByName(ContentTypeName);
            if (ct == null)
                throw new PackagingException("Unknown content type: " + ContentTypeName);

            var count = 0;

            foreach (var sourceContent in Search.ContentQuery.Query(ContentQuery).Nodes.Select(n => Content.Create(n)))
            {
                try
                {
                    if (!ct.IsInstaceOfOrDerivedFrom(sourceContent.ContentType.Name))
                    {
                        // currently only child type is allowed
                        Logger.LogWarningMessage(string.Format("Cannot change type {0} to {1} (Path: {2}).", sourceContent.ContentType.Name, ContentTypeName, sourceContent.Path));
                        continue;
                    }

                    if (sourceContent.Children.Any())
                        throw new PackagingException("Cannot change the type of a content that has children. Path: " + sourceContent.Path);

                    var targetName = Guid.NewGuid().ToString();
                    var parent = sourceContent.ContentHandler.Parent;
                    var targetContent = Content.CreateNew(ContentTypeName, parent, targetName);

                    // copy fields (skip Name and all the missing fields)
                    foreach (var field in sourceContent.Fields.Values.Where(f => 
                        string.Compare(f.Name, "Name", StringComparison.Ordinal) != 0 && 
                        targetContent.Fields.ContainsKey(f.Name)))
                    {
                        targetContent[field.Name] = field.GetData(false);
                    }

                    // save the new content
                    targetContent.Save();

                    // delete the original
                    sourceContent.ForceDelete();

                    // rename the target
                    targetContent["Name"] = sourceContent.Name;
                    targetContent.Save(SavingMode.KeepVersion);

                    count++;

                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }

            Logger.LogMessage("{0} content were changed to be {1}.", count, ContentTypeName);
        }
    }
}
