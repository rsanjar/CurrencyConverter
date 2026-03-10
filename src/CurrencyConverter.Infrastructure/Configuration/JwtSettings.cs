namespace CurrencyConverter.Infrastructure.Configuration;

/// <summary>
/// JWT Bearer authentication settings.
/// Supports both symmetric (SecretKey / HMAC-SHA256) and asymmetric (PublicKey / RSA) validation.
/// Bind to "JwtSettings" section in appsettings.json.
/// <para>
/// If <see cref="PublicKey"/> is provided (RSA PEM), asymmetric validation is used.
/// Otherwise, <see cref="SecretKey"/> is used for HMAC-SHA256 symmetric validation.
/// </para>
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// HMAC-SHA256 symmetric secret key. Used when no PublicKey is configured.
    /// Must be at least 32 characters long.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// RSA public key in PEM format (-----BEGIN PUBLIC KEY-----...).
    /// When provided, asymmetric RSA token validation is used instead of HMAC.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Expected token issuer claim value.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Expected token audience claim value.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token lifetime in minutes (used when issuing tokens).
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Returns true when asymmetric RSA validation is configured.
    /// </summary>
    public bool UseAsymmetricValidation => !string.IsNullOrWhiteSpace(PublicKey);
}
