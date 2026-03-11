using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using CurrencyConverter.Api.Configuration;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Api.Extensions;

public static class RateLimitingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers ASP.NET Core's built-in rate limiter with two named policies:
        /// <list type="bullet">
        ///   <item><see cref="RateLimitPolicies.Authenticated"/> — fixed-window per authenticated username.</item>
        ///   <item><see cref="RateLimitPolicies.Auth"/> — fixed-window per client IP for the token endpoint.</item>
        /// </list>
        /// All limits are read at request time via <see cref="IOptionsSnapshot{TOptions}"/> so that
        /// test overrides applied by <c>WebApplicationFactory.ConfigureWebHost</c> take effect.
        /// </summary>
        public IServiceCollection AddApiRateLimiting(IConfiguration configuration)
        {
            // Bind lazily — test factories can override individual keys after startup.
            services.Configure<RateLimitingOptions>(
                configuration.GetSection(RateLimitingOptions.SectionName));

            services.AddRateLimiter(options =>
            {
                // ── Rejection handler ─────────────────────────────────────────
                // Returns 429 with a Retry-After header instead of the default 503.
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";

                    var retrySeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? (int)retryAfter.TotalSeconds
                        : 60; // fallback: advise the client to wait one window

                    context.HttpContext.Response.Headers.RetryAfter =
                        retrySeconds.ToString(CultureInfo.InvariantCulture);

                    await context.HttpContext.Response.WriteAsync(
                        """{"error":"Too many requests. Please try again later."}""",
                        cancellationToken);
                };
                
                // ── Global per-IP ceiling (evaluated before any named policy) ────
                // Caps total throughput from a single IP regardless of how many
                // different user accounts are sending requests from that address.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var opts = httpContext.RequestServices
                        .GetRequiredService<IOptionsSnapshot<RateLimitingOptions>>().Value;

                    if (!opts.Enabled || !opts.GlobalIp.Enabled)
                        return RateLimitPartition.GetNoLimiter("global-nolimit");

                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"global-ip:{ip}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = opts.GlobalIp.PermitLimit,
                            Window = TimeSpan.FromSeconds(opts.GlobalIp.WindowSeconds),
                            QueueLimit = opts.GlobalIp.QueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true,
                        });
                });

                // ── Authenticated endpoints — per-user fixed window ────────────
                options.AddPolicy(RateLimitPolicies.Authenticated, httpContext =>
                {
                    var opts = httpContext.RequestServices
                        .GetRequiredService<IOptionsSnapshot<RateLimitingOptions>>().Value;

                    if (!opts.Enabled)
                        return RateLimitPartition.GetNoLimiter(RateLimitPolicies.Authenticated);

                    // Key by username; try both the raw JWT claim name ("name") and the
                    // mapped CLR claim type, then fall back to IP when both are absent.
                    var key = httpContext.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                              ?? httpContext.User.Identity?.Name
                              ?? httpContext.Connection.RemoteIpAddress?.ToString()
                              ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"user:{key}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = opts.Authenticated.PermitLimit,
                            Window = TimeSpan.FromSeconds(opts.Authenticated.WindowSeconds),
                            QueueLimit = opts.Authenticated.QueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true,
                        });
                });

                // ── Auth endpoint — per-IP fixed window (brute-force guard) ────
                options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
                {
                    var opts = httpContext.RequestServices
                        .GetRequiredService<IOptionsSnapshot<RateLimitingOptions>>().Value;

                    if (!opts.Enabled)
                        return RateLimitPartition.GetNoLimiter(RateLimitPolicies.Auth);

                    var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"auth:{ip}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = opts.Auth.PermitLimit,
                            Window = TimeSpan.FromSeconds(opts.Auth.WindowSeconds),
                            QueueLimit = opts.Auth.QueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true,
                        });
                });
            });

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Adds the rate-limiter middleware to the request pipeline.
        /// Must be placed after <c>UseAuthentication()</c> so the username claim
        /// is available for the per-user partition key.
        /// </summary>
        public WebApplication UseApiRateLimiting()
        {
            app.UseRateLimiter();
            return app;
        }
    }
}
