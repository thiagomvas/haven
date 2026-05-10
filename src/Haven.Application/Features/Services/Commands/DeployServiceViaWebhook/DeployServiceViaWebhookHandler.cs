using Haven.Application.Common;
using Haven.Application.Common.Interfaces;

namespace Haven.Application.Features.Services.Commands.DeployServiceViaWebhook;

public sealed class DeployServiceViaWebhookHandler(IDeployWebhookService deployWebhookService)
    : Haven.Application.Common.Messaging.ICommandHandler<DeployServiceViaWebhookCommand>
{
    public async ValueTask<Result> Handle(DeployServiceViaWebhookCommand request, CancellationToken cancellationToken)
    {
        return await deployWebhookService.TryEnqueueWithTokenAsync(request.Token, cancellationToken);
    }
}
