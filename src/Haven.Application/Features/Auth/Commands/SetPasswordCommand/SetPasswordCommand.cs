using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Auth.Commands.SetPasswordCommand;

public sealed class SetPasswordCommand : ICommand
{
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
