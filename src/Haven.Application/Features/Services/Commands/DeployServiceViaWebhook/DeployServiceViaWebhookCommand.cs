using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.DeployServiceViaWebhook;

public sealed class DeployServiceViaWebhookCommand : ICommand
{
    public string Token { get; set; } = string.Empty;
}
