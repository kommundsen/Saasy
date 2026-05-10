using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saasy.Tenancy.Domain.Integrators;

namespace Saasy.Tenancy.Infrastructure.Auth;

// Claim type constants used by downstream code to read Integrator identity.
public static class ApiKeyClaims
{
    public const string IntegratorId = "integrator_id";
    public const string IntegratorKind = "integrator_kind";
    public const string IntegratorTier = "integrator_tier";
}

internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IIntegratorRepository integrators)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "ApiKey";

    private readonly ILogger<ApiKeyAuthenticationHandler> _logger =
        loggerFactory.CreateLogger<ApiKeyAuthenticationHandler>();

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader))
        {
            _logger.LogInformation("Auth failed: missing_header");
            return AuthenticateResult.Fail("missing_header");
        }

        // Case-insensitive "ApiKey " prefix check
        if (!authHeader.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Auth failed: malformed_header (scheme={Scheme})",
                authHeader.Split(' ')[0]);
            return AuthenticateResult.Fail("malformed_header");
        }

        var secret = authHeader["ApiKey ".Length..].Trim();
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogInformation("Auth failed: malformed_header (empty secret)");
            return AuthenticateResult.Fail("malformed_header");
        }

        if (secret.Length < 4)
        {
            _logger.LogInformation("Auth failed: malformed_header (secret too short)");
            return AuthenticateResult.Fail("malformed_header");
        }

        var prefix = secret[^4..];
        var candidates = await integrators.GetByApiKeyPrefixAsync(prefix, Context.RequestAborted);

        if (candidates.Count == 0)
        {
            _logger.LogInformation("Auth failed: unknown_prefix (prefix={Prefix})", prefix);
            return AuthenticateResult.Fail("unknown_prefix");
        }

        // Scan ALL candidates regardless of match to prevent timing leaks across prefix
        // collisions. Accumulate results without short-circuiting on first match.
        Integrator? matched = null;
        var anyHashMatch = false;

        foreach (var integrator in candidates)
        {
            foreach (var apiKey in integrator.ApiKeys.Where(k => k.Last4 == prefix))
            {
                var verified = ApiKeyHasher.Verify(secret, apiKey.HashedSecret);
                if (verified)
                {
                    anyHashMatch = true;
                    if (!apiKey.IsRevoked && matched is null)
                    {
                        // Record the first valid (non-revoked) match; continue scanning.
                        matched = integrator;
                    }
                }
                // Always continue scanning -- never short-circuit on first match.
            }
        }

        if (!anyHashMatch)
        {
            _logger.LogInformation("Auth failed: hash_mismatch (prefix={Prefix})", prefix);
            return AuthenticateResult.Fail("hash_mismatch");
        }

        if (matched is null)
        {
            // Hash matched but only against revoked key(s).
            _logger.LogInformation("Auth failed: revoked_key (prefix={Prefix})", prefix);
            return AuthenticateResult.Fail("revoked_key");
        }

        var claims = new[]
        {
            new Claim(ApiKeyClaims.IntegratorId, matched.Id.Value.ToString()),
            new Claim(ApiKeyClaims.IntegratorKind, matched.Kind.ToString()),
            new Claim(ApiKeyClaims.IntegratorTier, matched.Tier.ToString()),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Return 401 with no body -- per acceptance criteria.
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}
