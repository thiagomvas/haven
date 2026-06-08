using Haven.Application.Common.Interfaces;

namespace Haven.Presentation.Api.Middleware;

public class ValidateSetupMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IHavenService havenService)
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments("/setup", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/setup", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/scalar") ||
            Path.HasExtension(path.Value))
        {
            await next(context);
            return;
        }

        if (await havenService.RequiresFirstTimeSetupAsync(context.RequestAborted))
        {
            context.Response.Redirect("/setup");
            return;
        }

        await next(context);
    }
}