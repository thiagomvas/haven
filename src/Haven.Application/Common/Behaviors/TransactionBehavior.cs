using Haven.Application.Common.Interfaces;
using Mediator;

namespace Haven.Application.Common.Behaviors;

public sealed class TransactionBehavior<TMessage, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken ct)
    {
        if (message is not ICommand and not ICommand<TResponse>)
            return await next(message, ct);

        var response = await next(message, ct);

        if (response is Result { IsFailure: true })
            return response;

        await unitOfWork.SaveChangesAsync(ct);
        return response;
    }
}