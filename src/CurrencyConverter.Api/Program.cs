using CurrencyConverter.Api.Middleware;
using CurrencyConverter.Api.Services;
using CurrencyConverter.Application;
using CurrencyConverter.Application.Common.Interfaces;
using CurrencyConverter.Infrastructure;
using Scalar.AspNetCore;
using TheTechLoop.HybridCache.Extensions;
using TheTechLoop.HybridCache.MediatR.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Caching ───────────────────────────────────────────────────────────────
//NOTE: This package belongs to me, it's not a third party library.
// TheTechLoop.HybridCache is added at the end to ensure it can wrap all
// MediatR handlers and other services for caching and cache invalidation.
builder.Services.AddTheTechLoopCache(builder.Configuration);

// Register CachingBehavior + CacheInvalidationBehavior AFTER AddApplication()
// so they sit innermost in the MediatR pipeline (closest to the handler).
builder.Services.AddTheTechLoopCacheBehaviors();

var app = builder.Build();

// Global exception handling — must be first in pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
//app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
