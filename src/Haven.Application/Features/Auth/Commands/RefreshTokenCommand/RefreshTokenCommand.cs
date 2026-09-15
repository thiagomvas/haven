using Haven.Application.Common.Contracts;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Auth.Commands.RefreshTokenCommand;

public sealed class RefreshTokenCommand : ICommand<AuthResponse>
{
    public string Token { get; set; } = string.Empty;
}