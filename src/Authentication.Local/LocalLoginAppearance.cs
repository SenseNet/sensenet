namespace SenseNet.Authentication.Local;

/// <summary>Public, per-repository login branding. No arbitrary HTML or CSS is accepted.</summary>
public sealed class LocalLoginAppearance
{
    public string Title { get; set; } = "Login to sensenet";
    public string? BackgroundImageUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string BackgroundColor { get; set; } = "#d4f3fa";
    public string BrandColor { get; set; } = "#38a9cb";
    public string ButtonColor { get; set; } = "#38a9cb";
    public string ButtonTextColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#343b43";
    public string PanelColor { get; set; } = "#ffffff";
}

public sealed class LocalPasswordRecoveryOptions
{
    public bool Enabled { get; set; }
    public bool RequireSmtpTls { get; set; } = true;
    /// <summary>Trusted Admin UI page, HTTPS or HTTP loopback for local development. Never supplied by the request.</summary>
    public string ResetUrl { get; set; } = "";
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public int MinimumPasswordLength { get; set; } = 12;
}
