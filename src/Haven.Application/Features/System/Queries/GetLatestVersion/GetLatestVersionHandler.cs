using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Queries.GetLatestVersion;

public sealed class GetLatestVersionHandler(IHavenVersionService havenVersionService)
    : IQueryHandler<GetLatestVersionQuery, LatestVersionDto>
{
    public async ValueTask<Result<LatestVersionDto>> Handle(GetLatestVersionQuery query, CancellationToken cancellationToken)
    {
        var result = await havenVersionService.GetLatestReleaseAsync(ct: cancellationToken);
        if (result.IsFailure)
            return result.Error;

        var latest = result.Value;
        var currentVersion = havenVersionService.CurrentVersion;

        return Result<LatestVersionDto>.Success(new LatestVersionDto
        {
            CurrentVersion = currentVersion.ToString(),
            LatestVersion = latest.Version.ToString(),
            IsUpdateAvailable = latest.Version > currentVersion,
            Name = latest.Name,
            HtmlUrl = latest.HtmlUrl,
            Body = latest.Body,
            Prerelease = latest.Prerelease,
            PublishedAt = latest.PublishedAt,
        });
    }
}
