using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration;

[Obsolete("Use SecurityOptions instead from the service collection.", true)]
public class Security : SnConfig
{
    private const string SectionName = "sensenet/security";

    public static bool EnablePasswordHashMigration { get; internal set; } = GetValue<bool>(SectionName, "EnablePasswordHashMigration");
    public static int PasswordHistoryFieldMaxLength { get; internal set; } = GetInt(SectionName, "PasswordHistoryFieldMaxLength", 10);
}
public class SecurityOptions
{
    private const string SectionName = "sensenet/security";

    public bool EnablePasswordHashMigration { get; set; }
    public int PasswordHistoryFieldMaxLength { get; set; } = 10;
}