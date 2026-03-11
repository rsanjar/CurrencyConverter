using System.Security.Cryptography;
using System.Text;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Infrastructure.Configuration;
using CurrencyConverter.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Polly;

namespace CurrencyConverter.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {

            // ── HTTP Clients ─────────────────────────────────────────────────
            services.AddFrankfurterClient(configuration);

            // ── CORS ─────────────────────────────────────────────────────────
            services.AddCorsPolicy(configuration);

            // ── JWT Bearer Authentication ─────────────────────────────────────
            services.AddJwtBearerAuthentication(configuration);

            return services;
        }
        
        private IServiceCollection AddFrankfurterClient(IConfiguration configuration)
        {
            var options = configuration
                .GetSection(FrankfurterOptions.SectionName)
                .Get<FrankfurterOptions>() ?? new FrankfurterOptions();

            // ── HTTP Clients ─────────────────────────────────────────────────
            services.AddHttpClient<IFrankfurterService, FrankfurterService>(client =>
                {
                    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                    // Timeout is governed by the resilience pipeline's TotalRequestTimeout;
                    // setting InfiniteTimeSpan prevents HttpClient from racing the pipeline.
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                })
                .AddStandardResilienceHandler(o =>
                {
                    // Total timeout covering all retries combined.
                    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

                    // Exponential back-off with jitter.
                    o.Retry.MaxRetryAttempts = options.RetryMaxAttempts;
                    o.Retry.Delay = TimeSpan.FromSeconds(options.RetryBaseDelaySeconds);
                    o.Retry.BackoffType = DelayBackoffType.Exponential;
                    o.Retry.UseJitter = true;

                    // Circuit breaker.
                    o.CircuitBreaker.FailureRatio = options.CircuitBreakerFailureRatio;
                    o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingDurationSeconds);
                    o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds);
                    o.CircuitBreaker.MinimumThroughput = options.CircuitBreakerMinimumThroughput;
                });

            return services;
        }

        private IServiceCollection AddCorsPolicy(IConfiguration configuration)
        {
            var cors = configuration.GetSection(CorsConfig.SectionName).Get<CorsConfig>() ?? new CorsConfig();

            services.AddCors(options =>
            {
                options.AddPolicy(cors.PolicyName, policy =>
                {
                    if (cors.AllowedOrigins.Length > 0)
                        policy.WithOrigins(cors.AllowedOrigins);
                    else
                        policy.AllowAnyOrigin();

                    policy.WithMethods(cors.AllowedMethods)
                        .WithHeaders(cors.AllowedHeaders);

                    if (cors.AllowCredentials && cors.AllowedOrigins.Length > 0)
                        policy.AllowCredentials();
                });
            });

            return services;
        }

        private IServiceCollection AddJwtBearerAuthentication(IConfiguration configuration)
        {
            var jwtSettings = configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>() ?? new JwtSettings();

            // Register as IOptions<JwtSettings> so other services (e.g., AuthController) can inject it.
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = !string.IsNullOrWhiteSpace(jwtSettings.Issuer),
                        ValidateAudience = !string.IsNullOrWhiteSpace(jwtSettings.Audience),
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = ResolveSigningKey(jwtSettings),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            return services;
        }

    }

    private static SecurityKey ResolveSigningKey(JwtSettings settings)
    {
        if (settings.UseAsymmetricValidation)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(settings.PublicKey);
            return new RsaSecurityKey(rsa);
        }

        var keyBytes = Encoding.UTF8.GetBytes(settings.SecretKey);
        return new SymmetricSecurityKey(keyBytes);
    }
}
