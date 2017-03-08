// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration
{
    public class RepositoryStructure : SnConfig
    {
        private const string SectionName = "sensenet/repositoryStructure";

        #region Backward compatibility

        // These properties are kept for compatibility reasons. These values should not be configured.

        public static string ContentTemplateFolderPath { get; internal set; } = GetString(SectionName, "ContentTemplateFolderPath", "/Root/ContentTemplates");
        public static string ImsFolderPath { get; internal set; } = GetString(SectionName, "IMSFolderPath", "/Root/IMS");
        public static string ResourceFolderPath { get; internal set; } = GetString(SectionName, "ResourceFolderPath", "/Root/Localization");
        public static string SkinRootFolderPath { get; internal set; } = GetString(SectionName, "SkinRootFolderPath", "/Root/Skins");
        public static string SkinGlobalFolderPath { get; internal set; } = GetString(SectionName, "SkinGlobalFolderPath", "/Root/Global");
        public static string PageTemplateFolderPath { get; internal set; } = GetString(SectionName, "PageTemplateFolderPath", "/Root/Global/pagetemplates");
        public static string ContentViewFolderName { get; internal set; } = GetString(SectionName, "ContentViewFolderName", "$skin/contentviews");
        public static string ContentViewGlobalFolderPath { get; internal set; } = GetString(SectionName, "ContentViewGlobalFolderPath", "/Root/Global/contentviews");
        public static string FieldControlTemplatesPath { get; internal set; } = GetString(SectionName, "FieldControlTemplatesPath", "$skin/fieldcontroltemplates");
        public static string CellTemplatesPath { get; internal set; } = GetString(SectionName, "CellTemplatesPath", "$skin/celltemplates");

        #endregion
    }
}
