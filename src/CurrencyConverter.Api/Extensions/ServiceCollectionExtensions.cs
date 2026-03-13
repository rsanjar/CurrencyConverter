using Asp.Versioning;
using CurrencyConverter.Api.Middleware;
using CurrencyConverter.Api.Services;
using CurrencyConverter.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace CurrencyConverter.Api.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        
        /// <summary>
        /// Configures OpenAPI documentation generation for the application.
        /// One OpenAPI document is registered per entry in <paramref name="versions"/>;
        /// each document is served at <c>/openapi/{version}.json</c> and appears as a
        /// separate entry in the Scalar version drop-down.
        /// </summary>
        /// <param name="title">The base title shown in both Scalar and the OpenAPI document.</param>
        /// <param name="versions">
        /// API version names to register (e.g. <c>["v1", "v2"]</c>).
        /// Defaults to <c>["v1"]</c> when <see langword="null"/>.
        /// </param>
        /// <param name="description">Optional description added to every version document.</param>
        public IServiceCollection AddCustomOpenApi(string title, string[]? versions = null, string description = "")
        {
            var versionList = versions ?? ["v1"];

            foreach (var version in versionList)
            {
                // Capture loop variable for use inside the lambda.
                var v = version;

                services.AddOpenApi(v, options =>
                {
                    options.AddDocumentTransformer((document, _, _) =>
                    {
                        document.Info.Title = versionList.Length > 1 ? $"{title} {v.ToUpper()}" : title;
                        document.Info.Version = v;

                        if (!string.IsNullOrEmpty(description))
                            document.Info.Description = description;

                        document.Components ??= new OpenApiComponents();
                        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                        {
                            In = ParameterLocation.Header,
                            Name = "Authorization",
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            BearerFormat = "JWT",
                            Description = "JWT Authorization header using the Bearer scheme. (only the token, no 'Bearer' prefix)"
                        };

                        return Task.CompletedTask;
                    });

                    // Apply Bearer requirement per-operation; skip for [AllowAnonymous] endpoints.
                    options.AddOperationTransformer((operation, context, _) =>
                    {
                        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                        bool isAnonymous = metadata.Any(m => m is AllowAnonymousAttribute);

                        if (!isAnonymous)
                        {
                            operation.Security = [new OpenApiSecurityRequirement
                            {
                                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
                            }];
                        }

                        return Task.CompletedTask;
                    });
                });
            }

            return services;
        }

        /// <summary>
        /// Registers URL-segment API versioning with a default version of 1.0.
        /// <para>
        /// All versioned routes use the <c>/api/v{version}/</c> prefix.
        /// Requests that omit the version segment are treated as v1.0.
        /// The <c>api-supported-versions</c> and <c>api-deprecated-versions</c> headers
        /// are included in every response when <c>ReportApiVersions</c> is <c>true</c>.
        /// </para>
        /// <para>
        /// <see cref="Asp.Versioning.ApiExplorerOptions.SubstituteApiVersionInUrl"/> is set to
        /// <see langword="true"/> so the OpenAPI document replaces <c>{version:apiVersion}</c> with
        /// the concrete version value (e.g. <c>v1</c>), removing the manual path-parameter entry
        /// from the Scalar UI.
        /// </para>
        /// </summary>
        public IServiceCollection AddVersioning()
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                // Format as 'v1', 'v2', … matching the route segment convention.
                options.GroupNameFormat = "'v'VVV";

                // Substitute {version:apiVersion} with the actual value in every URL template,
                // so Scalar shows /api/v1/... instead of /api/v{version}/... and users never
                // have to type the version into a path-parameter box.
                options.SubstituteApiVersionInUrl = true;
            });

            return services;
        }

        /// <summary>
        /// Registers the current user accessor services required to resolve the authenticated user
        /// identity from the active HTTP context.
        /// </summary>
        /// <remarks>Registers <see cref="IHttpContextAccessor"/> and binds <see cref="ICurrentUserService"/>
        /// to <see cref="CurrentUserService"/>. Call this whenever a service or handler needs access to
        /// <c>ICurrentUserService</c> via dependency injection.</remarks>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddCurrentUser()
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }

        /// <summary>
        /// Configures the application to process forwarded headers from reverse proxies, specifically the
        /// X-Forwarded-For and X-Forwarded-Proto headers.
        /// </summary>
        /// <remarks>This method clears any previously configured known IP networks and proxies, ensuring
        /// that only the specified forwarded headers are processed. Use this method when deploying behind a reverse
        /// proxy to correctly interpret client information from forwarded headers.</remarks>
        /// <returns>An IServiceCollection instance that can be used for further configuration or method chaining.</returns>
        public IServiceCollection ForwardHeaders()
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Maps one OpenAPI endpoint per registered version and configures the Scalar API reference UI.
        /// Each version appears as a separate entry in the Scalar version drop-down.
        /// Also redirects <c>GET /</c> to the Scalar UI for the first version.
        /// </summary>
        /// <param name="title">The base title shown in the Scalar UI header.</param>
        /// <param name="versions">
        /// API version names to expose (e.g. <c>["v1", "v2"]</c>).
        /// Must match the names passed to <see cref="AddCustomOpenApi"/>.
        /// Defaults to <c>["v1"]</c> when <see langword="null"/>.
        /// </param>
        /// <param name="theme">The visual theme for the Scalar UI. Defaults to <see cref="ScalarTheme.Moon"/>.</param>
        /// <param name="layout">The page layout for the Scalar UI. Defaults to <see cref="ScalarLayout.Modern"/>.</param>
        /// <param name="target">The HTTP client language target for code samples. Defaults to <see cref="ScalarTarget.JavaScript"/>.</param>
        /// <param name="client">The HTTP client library for code samples. Defaults to <see cref="ScalarClient.Fetch"/>.</param>
        public WebApplication MapCustomOpenApi(string title = "API", string[]? versions = null,
            ScalarTheme theme = ScalarTheme.Moon, ScalarLayout layout = ScalarLayout.Modern,
            ScalarTarget target = ScalarTarget.JavaScript, ScalarClient client = ScalarClient.Fetch)
        {
            var versionList = versions ?? ["v1"];

            // Serves /openapi/{documentName}.json for every registered document.
            app.MapOpenApi().CacheOutput();

            app.MapScalarApiReference(options =>
            {
                options.Theme = theme;
                options.Title = title;
                options.DocumentDownloadType = DocumentDownloadType.Json;
                options.Layout = layout;
                options.DefaultHttpClient = KeyValuePair.Create(target, client);
                options.AddPreferredSecuritySchemes("Bearer");

                // Each call to AddDocument adds one entry to the Scalar version drop-down.
                foreach (var version in versionList)
                    options.AddDocument($"/openapi/{version}.json", $"{title} {version.ToUpper()}");
            });

            app.MapGet("/", () => Results.Redirect($"/scalar/{versionList[0]}"));

            return app;
        }

        /// <summary>
        /// Adds the <see cref="GlobalExceptionHandlingMiddleware"/> to the request pipeline,
        /// catching unhandled exceptions and returning structured <c>ProblemDetails</c> responses.
        /// </summary>
        /// <remarks>Register this early in the pipeline, before routing and authorization middleware,
        /// so that exceptions from any subsequent middleware are captured.</remarks>
        /// <returns>The <see cref="WebApplication"/> instance for further chaining.</returns>
        public WebApplication UseGlobalExceptionHandler()
        {
            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

            return app;
        }

        public WebApplication MapCustomHealthChecks()
        {
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
            app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = _ => true });

            return app;
        }
    }
}