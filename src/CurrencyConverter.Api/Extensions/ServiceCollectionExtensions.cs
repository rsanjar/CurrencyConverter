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
        /// Configures OpenAPI documentation generation for the application with the specified title, version, and
        /// description.
        /// </summary>
        /// <remarks>This method customizes the OpenAPI document by setting its general information and
        /// modifying operation security requirements based on the presence of the <see
        /// cref="AllowAnonymousAttribute"/>. Operations marked as anonymous will not require authorization in the
        /// generated documentation.</remarks>
        /// <param name="title">The title to display in the generated OpenAPI documentation.</param>
        /// <param name="version">The version of the OpenAPI documentation. The default value is "v1".</param>
        /// <param name="description">A brief description of the API to include in the OpenAPI documentation.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance, enabling further configuration of the service collection.</returns>
        public IServiceCollection AddCustomOpenApi(string title, string version = "v1", string description = "")
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, _) =>
                {
                    document.Info.Title = title;
                    document.Info.Version = version;
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

                // Apply Bearer requirement per-operation; skip for [AllowAnonymous] endpoints
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
        /// Maps the OpenAPI endpoint and configures the Scalar API reference UI.
        /// Also redirects <c>GET /</c> to the Scalar UI.
        /// </summary>
        /// <remarks>Call this in the application pipeline after <c>UseRouting</c> and <c>UseAuthorization</c>.
        /// The OpenAPI document output is cached using output caching.</remarks>
        /// <param name="title">The title shown in the Scalar UI header.</param>
        /// <param name="version">The OpenAPI document version segment used in the Scalar route. Defaults to "v1".</param>
        /// <param name="theme">The visual theme for the Scalar UI. Defaults to <see cref="ScalarTheme.Moon"/>.</param>
        /// <param name="layout">The page layout for the Scalar UI. Defaults to <see cref="ScalarLayout.Modern"/>.</param>
        /// <param name="target">The HTTP client language target for code samples. Defaults to <see cref="ScalarTarget.JavaScript"/>.</param>
        /// <param name="client">The HTTP client library for code samples. Defaults to <see cref="ScalarClient.Fetch"/>.</param>
        /// <returns>The <see cref="WebApplication"/> instance for further chaining.</returns>
        public WebApplication MapCustomOpenApi(string title = "API", string version = "v1",
            ScalarTheme theme = ScalarTheme.Moon, ScalarLayout layout = ScalarLayout.Modern,
            ScalarTarget target = ScalarTarget.JavaScript, ScalarClient client = ScalarClient.Fetch)
        {
            app.MapOpenApi().CacheOutput();

            app.MapScalarApiReference(options =>
            {
                options.Theme = theme;
                options.Title = title;
                options.DocumentDownloadType = DocumentDownloadType.Json;
                options.Layout = layout;
                options.DefaultHttpClient = KeyValuePair.Create(target, client);
                options.AddPreferredSecuritySchemes("Bearer");
            });

            app.MapGet("/", () => Results.Redirect($"/scalar/{version}"));

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