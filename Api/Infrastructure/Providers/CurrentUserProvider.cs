using Api.Common;
using System.Security.Claims;

namespace Api.Infrastructure.Providers;

internal sealed class CurrentUserProvider(IHttpContextAccessor contextAccessor) : ICurrentUserProvider
{
    public Guid UserId 
        => Guid.TryParse(contextAccessor?.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
        ? userId
        : throw new InvalidOperationException("User ID not found in the current context.");
    public string Email 
        => contextAccessor?.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value 
        ?? throw new InvalidOperationException("User email not found in the current context.");

    public bool IsAdmin
        => contextAccessor?.HttpContext?.User.IsInRole(Roles.Admin) 
        ?? throw new InvalidOperationException("User roles not found in the current context.");
}