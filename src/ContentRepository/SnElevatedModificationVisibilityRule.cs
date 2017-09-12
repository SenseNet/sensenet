using SenseNet.ContentRepository.Storage;
using System.Linq;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Implements a general rule that controls the visibility of content modification
    /// when a content is saved by the system user.
    /// Visible modification means: ModificationDate and ModifiedBy is updated before saving the content.
    /// </summary>
    public class SnElevatedModificationVisibilityRule : ElevatedModificationVisibilityRule
    {
        private readonly string[] _systemFileExtensions = { "aspx", "ascx", "cshtml", "vbhtml", "js", "css" };

        /// <summary>
        /// Returns true if the content is file and its name extension is one of the followings:
        /// "aspx", "ascx", "cshtml", "vbhtml", "js", "css".
        /// </summary>
        protected override bool IsModificationVisible(Node content)
        {
            if (content.NodeType.IsInstaceOfOrDerivedFrom(typeof(File).Name))
            {
                var segments = content.Name.Split('.');
                if (segments.Length > 1)
                {
                    var ext = segments[segments.Length - 1].ToLowerInvariant();
                    return this._systemFileExtensions.Contains(ext);
                }
            }
            return base.IsModificationVisible(content);
        }
    }
}
