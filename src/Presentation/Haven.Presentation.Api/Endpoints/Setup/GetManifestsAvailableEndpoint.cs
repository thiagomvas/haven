using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Configuration;

using Microsoft.Extensions.Options;

namespace Haven.Presentation.Api.Endpoints.Setup;

public sealed record ManifestsAvailableResult(bool Available, int ProjectCount);

public sealed class GetManifestsAvailableEndpoint(IOptionsMonitor<ManifestsOptions> manifestsOptions)
    : EndpointWithoutRequest<ApiResponse<ManifestsAvailableResult>>
{
    public override void Configure()
    {
        Get("/setup/manifests-available");
        AllowAnonymous();
        Options(x => x.WithTags("Setup"));
        Summary(s =>
        {
            s.Summary = "Check if manifests are available";
            s.Description = "Returns whether the local manifests directory contains any project data that can be restored during initial setup.";
            s[200] = "Availability status returned";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var manifestsPath = manifestsOptions.CurrentValue.ManifestsPath;
        var projectsPath = Path.Combine(manifestsPath, "projects");

        int projectCount = 0;
        if (Directory.Exists(projectsPath))
        {
            projectCount = Directory.EnumerateDirectories(projectsPath)
                .Count(d => Directory.EnumerateFiles(d, "*.yaml", SearchOption.TopDirectoryOnly).Any());
        }

        var result = new ManifestsAvailableResult(projectCount > 0, projectCount);
        await HttpContext.Response.SendAsync(new ApiResponse<ManifestsAvailableResult>(true, result), 200, cancellation: ct);
    }
}
