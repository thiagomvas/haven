using Version = Haven.Domain.ValueObjects.Version;

namespace Haven.Application.Common.Contracts;

public sealed record HavenReleaseInfo(
    Version Version,
    string? Name,
    string? HtmlUrl,
    string? Body,
    bool Prerelease,
    DateTimeOffset? PublishedAt);