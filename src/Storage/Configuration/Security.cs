﻿// ReSharper disable once CheckNamespace
namespace SenseNet.Configuration;

public class Security : SnConfig
{
    private const string SectionName = "sensenet/security";

    public static bool EnablePasswordHashMigration { get; internal set; } = GetValue<bool>(SectionName, "EnablePasswordHashMigration");
    public static int PasswordHistoryFieldMaxLength { get; internal set; } = GetInt(SectionName, "PasswordHistoryFieldMaxLength", 10);
}