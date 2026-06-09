using System.Security.Claims;

using Haven.Application.Common.Interfaces;

using Microsoft.AspNetCore.Http;

namespace Haven.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsAdmin
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user != null &&
                   (user.IsInRole("Admin") || user.HasClaim(c => c.Type == "role" && c.Value == "Admin"));
        }
    }
}