using System;
using System.Linq;
using SenseNet.ContentRepository;
using SenseNet.ApplicationModel;

namespace SenseNet.Portal.OData.Actions
{
    /// <summary>
    /// OData action that restores an old version of a content.
    /// </summary>
    public sealed class RestoreVersionAction : UrlAction
    {
        public override bool IsHtmlOperation { get; } = true;
        public override bool IsODataOperation { get; } = true;
        public override bool CausesStateChange { get; } = true;
        public override ActionParameter[] ActionParameters { get; } = { new ActionParameter("version", typeof(string), true), };

        public override object Execute(Content content, params object[] parameters)
        {
            var version = parameters.FirstOrDefault() as string;

            // Perform checks
            if (version == null)
                throw new Exception("Can't restore old version because the version parameter is empty.");
            if (!(content.ContentHandler is GenericContent))
                throw new Exception(string.Format("Can't restore old version of content '{0}' because its content handler is not a GenericContent. It needs to inherit from GenericContent for collaboration feature support.", content.Path));

            // Append 'V' if it is not there
            var versionText = version.StartsWith("V")
                                  ? version
                                  : string.Concat("V", version);

            // Find old version on the content

            // Try exact match on version string - example input: 1.0.A or V1.0.A
            var oldVersion = content.Versions.AsEnumerable().SingleOrDefault(n => n.Version.VersionString == versionText) as GenericContent;

            // Try adding status code - example input: 1.0 or V1.0
            if (oldVersion == null)
                oldVersion = content.Versions.AsEnumerable().SingleOrDefault(n => n.Version.VersionString == string.Concat(versionText, ".", n.Version.Status.ToString()[0])) as GenericContent;

            // Throw exception if still not found
            if (oldVersion == null)
                throw new Exception(string.Format("The requested version '{0}' does not exist on content '{1}'.", version, content.Path));

            // Restore old version
            oldVersion.Save();

            // Return actual state of content
            return Content.Load(content.Id);
        }
    }
}
