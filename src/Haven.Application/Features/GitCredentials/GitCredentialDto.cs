using Haven.Domain;

namespace Haven.Application.Features.GitCredentials;

public class GitCredentialDto
{
    public Guid Id { get; set; }
    public GitProviderType ProviderType { get; set; }
    public string? HostUrl { get; set; }
    public GitAuthMethod AuthMethod { get; set; }
    public string DisplayName { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset LastValidatedAt { get; set; }
}