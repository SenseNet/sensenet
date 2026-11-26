#nullable enable
using System;

namespace SenseNet.OData.Operations.IndexIntegrity;

public sealed class IndexIntegrityOptions
{
    public bool Enabled { get; set; }
    public bool KeepPreviousChecks { get; set; }
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(2);
    public long MaxRunBytes { get; set; } = 8L * 1024 * 1024 * 1024;
    public long MaxTotalBytes { get; set; } = 16L * 1024 * 1024 * 1024;
    public long MinimumFreeBytes { get; set; } = 128L * 1024 * 1024;
}
