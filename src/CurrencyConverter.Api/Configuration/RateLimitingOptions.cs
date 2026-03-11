namespace CurrencyConverter.Api.Configuration;

/// <summary>
/// API rate-limiting configuration. Bind to the <c>"RateLimiting"</c> section
/// in <c>appsettings.json</c>.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Master switch. Set to <c>false</c> to disable all rate limiting
    /// (useful in development / integration-test environments).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Fixed-window policy applied to authenticated API endpoints,
    /// partitioned by the authenticated username.
    /// </summary>
    public RateLimitWindowOptions Authenticated { get; set; } = new();

    /// <summary>
    /// Fixed-window policy applied to the <c>/api/auth/token</c> endpoint,
    /// partitioned by client IP address to prevent brute-force attacks.
    /// </summary>
    public RateLimitWindowOptions Auth { get; set; } = new();

    /// <summary>
    /// Global fixed-window ceiling applied to <b>every</b> request, partitioned by
    /// client IP address.  This is evaluated before any named policy, so it caps the
    /// total throughput from a single IP regardless of how many different user accounts
    /// are making requests from that IP.
    /// </summary>
    public RateLimitWindowOptions GlobalIp { get; set; } = new() { PermitLimit = 200 };
}

/// <summary>
/// Parameters for a single fixed-window rate limit policy.
/// </summary>
public sealed class RateLimitWindowOptions
{
    /// <summary>
    /// Whether this individual policy is active.
    /// Set to <c>false</c> to bypass this policy while leaving others in place.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Maximum requests allowed within <see cref="WindowSeconds"/>.</summary>
    public int PermitLimit { get; set; } = 60;

    /// <summary>Length of the fixed window in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Number of requests to queue when the limit is exceeded before rejecting.
    /// Use <c>0</c> to reject immediately (no queuing).
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}

/// <summary>
/// Named rate-limit policy identifiers for use with
/// <see cref="Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute"/>.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Per-user fixed-window policy for authenticated API endpoints.
    /// Partitioned by JWT username claim.
    /// </summary>
    public const string Authenticated = "rl-authenticated";

    /// <summary>
    /// Per-IP fixed-window policy for the authentication endpoint.
    /// Limits token-request attempts to prevent credential brute-forcing.
    /// </summary>
    public const string Auth = "rl-auth";
}
