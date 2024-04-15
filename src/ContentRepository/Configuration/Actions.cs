// ReSharper disable once CheckNamespace

using System;

namespace SenseNet.Configuration
{
    [Obsolete("Don't use anymore", true)]
    public class Actions : SnConfig
    {
        private const string SectionName = "sensenet/actions";

        public static string DefaultActionType { get; internal set; } = GetString(SectionName, "DefaultActionType", "UrlAction");
    }
}
