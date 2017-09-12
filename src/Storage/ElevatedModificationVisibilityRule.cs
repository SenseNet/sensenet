using SenseNet.Configuration;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Defines a rule that can control the modification visibility when a content is saved by the system user.
    /// Visible modification means: ModificationDate and ModifiedBy is updated before saving the content.
    /// </summary>
    public class ElevatedModificationVisibilityRule
    {
        private static ElevatedModificationVisibilityRule Instance => Providers.Instance.ElevatedModificationVisibilityRuleProvider;

        internal static bool EvaluateRule(Node node)
        {
            return Instance?.IsModificationVisible(node) ?? false;
        }

        /// <summary>
        /// Returns true if the system user's modification is visible.
        /// It means: ModificationDate and ModifiedBy is updated before saving the content.
        /// This method always returns with false but overridable to customize.
        /// </summary>
        protected virtual bool IsModificationVisible(Node content)
        {
            return false;
        }
    }
}
