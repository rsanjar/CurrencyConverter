using Asp.Versioning;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CurrencyConverter.Api.Configuration;
using CurrencyConverter.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyConverter.Api.Controllers;

/// <summary>
/// Issues JWT access tokens for testing.
/// <para>
/// <b>Development only.</b> Configure <c>JwtSettings:TestUsername</c> and
/// <c>JwtSettings:TestPassword</c> in <c>appsettings.Development.json</c> to enable.
/// Leave those fields empty in production to disable credential validation entirely.
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public class AuthController(IOptions<JwtSettings> jwtOptions) : ControllerBase
{
    /// <summary>
    /// Issues a JWT access token for the provided credentials.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Token([FromBody] LoginRequest request)
    {
        var settings = jwtOptions.Value;

        // When TestUsername is not configured the endpoint is disabled — return 401 for every request.
        // Configure JwtSettings:TestUsername and JwtSettings:TestPassword (e.g. in appsettings.Development.json
        // or via environment variables / secrets) to enable this endpoint.
        if (string.IsNullOrEmpty(settings.TestUsername))
            return Unauthorized(new { error = "Token endpoint is not enabled in this environment." });

        if (!request.Username.Equals(settings.TestUsername, StringComparison.OrdinalIgnoreCase) ||
            request.Password != settings.TestPassword)
            return Unauthorized(new { error = "Invalid credentials." });

        var token = GenerateToken(settings, request.Username);

        return Ok(new TokenResponse(token, settings.ExpirationMinutes * 60));
    }

    private static string GenerateToken(JwtSettings settings, string username)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "user")
        };

        var keyBytes = Encoding.UTF8.GetBytes(settings.SecretKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>Login credentials.</summary>
public record LoginRequest(string Username, string Password);

/// <summary>JWT token response.</summary>
public record TokenResponse(string Token, int ExpiresIn);
