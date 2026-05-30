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
}
