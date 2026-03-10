using System.Security.Cryptography;
using System.Text;
using CurrencyConverter.Domain.Interfaces;
using CurrencyConverter.Infrastructure.Configuration;
using CurrencyConverter.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyConverter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(FrankfurterOptions.SectionName)
            .Get<FrankfurterOptions>() ?? new FrankfurterOptions();

        // ── HTTP Clients ─────────────────────────────────────────────────
        services.AddHttpClient<IFrankfurterService, FrankfurterService>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // ── JWT Bearer Authentication ─────────────────────────────────────
        services.AddJwtBearerAuthentication(configuration);

        return services;
    }

    private static IServiceCollection AddJwtBearerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>() ?? new JwtSettings();

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
