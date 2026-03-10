namespace CurrencyConverter.Infrastructure.Configuration;

/// <summary>
/// CORS policy configuration. Bind to the "Cors" section in appsettings.json.
/// </summary>
public sealed class CorsConfig
{
    public const string SectionName = "Cors";
    public const string DefaultPolicyName = "DefaultPolicy";

    public string PolicyName { get; set; } = DefaultPolicyName;

    /// <summary>
    /// Allowed origins. Leave empty to allow any origin (development only).
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "OPTIONS"];

    public string[] AllowedHeaders { get; set; } = ["Content-Type", "Authorization"];

    /// <summary>
    /// Allow credentials. Only takes effect when <see cref="AllowedOrigins"/> is non-empty.
    /// </summary>
    public bool AllowCredentials { get; set; }
}
