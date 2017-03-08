using System.Collections.Generic;

// ReSharper disable once CheckNamespace
// ReSharper disable RedundantTypeArgumentsOfMethod
namespace SenseNet.Configuration
{
    public class Webdav : SnConfig
    {
        private const string SectionName = "sensenet/webdav";

        public static string[] WebdavEditExtensions { get; internal set; } = GetList<string>(SectionName, "WebdavEditExtensions",
            new List<string>(new[]
            {
                ".doc", ".docx", ".xls", ".xlsx", ".xlsm", ".xltx", ".ods", ".odt",
                ".odp", ".ppt", ".pptx", ".ppd", ".pps", ".ppsx", ".rtf", ".mpp"
            })).ToArray();

        public static string[] MockExistingFiles { get; internal set; } = GetListOrEmpty<string>(SectionName, "MockExistingFiles",
            new List<string>(new[] { "desktop.ini", "Thumbs.db", "wdmaud.drv", "foo", "MSGRHU32.ini" }))
            .ToArray();
        public static bool AutoCheckoutFiles { get; internal set; } = GetValue<bool>(SectionName, "AutoCheckoutFiles");
    }
}
