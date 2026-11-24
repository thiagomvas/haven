using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ApiTokens.Commands;

[AuthenticatedOnly(AllowApiTokens = false)]
public sealed class RevokeApiTokenCommand : ICommand
{
    public Guid Id { get; set; }
}