namespace CryptoTaxHelper.Api;

/// <summary>
/// HTTP boundary limits, bound from the "HttpLimits" configuration section.
/// Adjust the values in appsettings.json; defaults here apply when a key is absent.
/// </summary>
public sealed class HttpLimitsOptions
{
    public UploadLimitOptions Upload { get; set; } = new();
    public RateLimitsOptions RateLimits { get; set; } = new();
}

public sealed class UploadLimitOptions
{
    // Maximum size of the uploaded broker CSV file, in bytes. Default 10 MiB (10485760).
    // Adjustable: change HttpLimits:Upload:MaxBytes in appsettings.json.
    public long MaxBytes { get; set; } = 10 * 1024 * 1024;
}

public sealed class RateLimitsOptions
{
    public RateLimitRule GenerateReport { get; set; } = new() { PermitLimit = 20, WindowSeconds = 60 };
    public RateLimitRule DownloadReport { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };
}

public sealed class RateLimitRule
{
    public int PermitLimit { get; set; } = 20;
    public int WindowSeconds { get; set; } = 60;
}
