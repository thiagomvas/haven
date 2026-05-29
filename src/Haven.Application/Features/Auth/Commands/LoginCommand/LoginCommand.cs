using Haven.Application.Common.Contracts;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Auth.Commands.LoginCommand;

public class LoginCommand : ICommand<AuthResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
