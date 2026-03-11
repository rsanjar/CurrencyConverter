namespace CurrencyConverter.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the Frankfurter API HTTP client.
/// </summary>
public class FrankfurterOptions
{
    public const string SectionName = "Frankfurter";

    /// <summary>
    /// Base URL of the Frankfurter API (default: https://api.frankfurter.app).
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.frankfurter.app";

    /// <summary>
    /// Overall request timeout in seconds covering all retry attempts (default: 90).
    /// Set this higher than a single attempt timeout to allow retries to complete.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 90;

    // ── Retry ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Maximum number of retry attempts on transient failures (default: 3).
    /// </summary>
    public int RetryMaxAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in seconds for the exponential back-off between retries (default: 1).
    /// Actual delay is: BaseDelay * 2^attempt ± jitter.
    /// </summary>
    public double RetryBaseDelaySeconds { get; set; } = 1.0;

    // ── Circuit Breaker ──────────────────────────────────────────────────────

    /// <summary>
    /// Failure ratio (0–1) within the sampling window that trips the circuit (default: 0.5).
    /// </summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Duration in seconds of the sliding window used to calculate the failure ratio (default: 30).
    /// </summary>
    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Duration in seconds the circuit stays open before allowing a probe request (default: 30).
    /// </summary>
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Minimum number of requests in the sampling window before the circuit can trip (default: 10).
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
}
