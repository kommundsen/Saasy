using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Saasy.Tenancy.Application;
using Saasy.Tenancy.Infrastructure.Auth;

namespace Saasy.Tenancy.Infrastructure.Audit;

// Resolves the current actor from the authenticated HttpContext for API requests.
// Falls back to "system:bootstrap" when no authenticated identity is present (e.g. Integrator.Create bootstrap).
internal sealed class HttpContextCurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    public string Actor
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return "system:bootstrap";

            var integratorId = user.FindFirstValue(ApiKeyClaims.IntegratorId);
            return integratorId is not null
                ? $"integrator:{integratorId}"
                : "system:bootstrap";
        }
    }
}
