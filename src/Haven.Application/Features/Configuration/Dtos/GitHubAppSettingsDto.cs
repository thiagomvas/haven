namespace Haven.Application.Features.Configuration.Dtos;

public sealed record GitHubAppSettingsDto(string ClientId, string RedirectUri, bool IsConfigured);