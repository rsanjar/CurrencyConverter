using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Security.Claims;

namespace CurrencyConverter.Api.Extensions;

public static class ObservabilityExtensions
{
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Configures Serilog from appsettings.json (Serilog section).
        /// Enriches with machine name, environment, thread, and process info.
        /// Pushes structured logs to Grafana Loki when configured.
        /// </summary>
        public WebApplicationBuilder AddSerilog()
        {
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithThreadId()
                    .Enrich.WithProcessId();
            });

            return builder;
        }

        /// <summary>
        /// Registers OpenTelemetry tracing and metrics.
        /// - Traces: ASP.NET Core, HttpClient → exported via OTLP to Grafana Tempo
        /// - Metrics: ASP.NET Core, HttpClient, runtime → exposed on /metrics for Prometheus scraping
        /// </summary>
        public WebApplicationBuilder AddOpenTelemetry(params string[] additionalMeterNames)
        {
            var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "currency-converter-svc";
            var serviceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
            var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

            builder.Services
                .AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(
                        serviceName: serviceName,
                        serviceVersion: serviceVersion)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = builder.Environment.EnvironmentName
                    }))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    }))
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddPrometheusExporter();

                    foreach (var meterName in additionalMeterNames)
                        metrics.AddMeter(meterName);
                });

            return builder;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Configures Serilog request logging with enriched diagnostics context.
        /// Stamps each log entry with client IP, User-Agent, host, and user ID.
        /// Must be called after <c>UseForwardedHeaders</c> so the real client IP is resolved.
        /// </summary>
        public WebApplication UseRequestLogging()
        {
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("ClientIp",
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                    diagnosticContext.Set("UserAgent",
                        httpContext.Request.Headers.UserAgent.ToString());
                    diagnosticContext.Set("RequestHost",
                        httpContext.Request.Host.Value ?? "unknown");
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);

                    if (httpContext.Request.QueryString.HasValue)
                        diagnosticContext.Set("QueryString", httpContext.Request.QueryString.Value);

                    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userId is not null)
                        diagnosticContext.Set("UserId", userId);
                };
            });

            return app;
        }
    }
}
