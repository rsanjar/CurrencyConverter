using CurrencyConverter.Api.Extensions;
using CurrencyConverter.Application;
using CurrencyConverter.Infrastructure;
using CurrencyConverter.Infrastructure.Configuration;
using TheTechLoop.HybridCache.Extensions;
using TheTechLoop.HybridCache.MediatR.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Observability — Serilog replaces the default logging provider
builder.AddSerilog();

// Observability — OpenTelemetry traces and metrics
//builder.AddOpenTelemetry();

// Forward X-Forwarded-For / X-Forwarded-Proto from Traefik so Request.Scheme is "https"
builder.Services.ForwardHeaders();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCustomOpenApi("Currency Converter API");
builder.Services.AddCurrentUser();

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Rate Limiting ─────────────────────────────────────────────────────────
builder.Services.AddApiRateLimiting(builder.Configuration);

// ── Caching ───────────────────────────────────────────────────────────────
//NOTE: This package belongs to me, it's not a third party library.
// TheTechLoop.HybridCache is added at the end to ensure it can wrap all
// MediatR handlers and other services for caching and cache invalidation.
builder.Services.AddTheTechLoopCache(builder.Configuration);

// Register CachingBehavior + CacheInvalidationBehavior AFTER AddApplication()
// so they sit innermost in the MediatR pipeline (closest to the handler).
builder.Services.AddTheTechLoopCacheBehaviors();

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"));

var app = builder.Build();

// Forwarded headers must be the first middleware so every subsequent piece of
// middleware (including OpenAPI/Scalar server-URL generation) sees the real scheme.
app.UseForwardedHeaders();

// Global exception handling — must be first in pipeline
app.UseGlobalExceptionHandler();

// Serilog request logging — after exception handler so errors are still captured
app.UseRequestLogging();

// Prometheus metrics scrape endpoint
//app.MapPrometheusScrapingEndpoint();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.MapCustomOpenApi("Currency Converter API");
}

// Health check endpoints
app.MapCustomHealthChecks();

app.UseHttpsRedirection();

// CORS — must be between routing and authorization
var corsPolicy = builder.Configuration.GetSection(CorsConfig.SectionName)
    .GetValue<string>(nameof(CorsConfig.PolicyName)) ?? CorsConfig.DefaultPolicyName;

app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseApiRateLimiting();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposes the generated Program class to the integration test project.
public partial class Program { }
