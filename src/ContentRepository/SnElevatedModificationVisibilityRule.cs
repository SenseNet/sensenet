using SenseNet.ContentRepository.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository
{
    /// <summary>
    /// Implements a general rule for the Sense/Net that controls the visibility of the content modification
    /// when a content is saved by the system user.
    /// Visible modification means: ModificationDate and ModifiedBy is updated before saving the content.
    /// </summary>
    public class SnElevatedModificationVisibilityRule : ElevatedModificationVisibilityRule
    {
        private string[] systemFileExtensions = new[] { "aspx", "ascx", "cshtml", "vbhtml", "js", "css" };

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
                    return this.systemFileExtensions.Contains(ext);
                }
            }
            return base.IsModificationVisible(content);
        }
    }
}
