using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ApiTokens.Commands;

[AuthenticatedOnly(AllowApiTokens = false)]
public sealed class GenerateApiTokenCommand : ICommand<GenerateApiTokenResult>
{
    public string Name { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = [];
    public TimeSpan? Expiration { get; set; }
}