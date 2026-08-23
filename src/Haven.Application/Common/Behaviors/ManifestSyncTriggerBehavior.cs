using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

using Mediator;

namespace Haven.Application.Common.Behaviors;

/// <summary>
/// Requests a debounced manifest resync after any <see cref="IMutatesManifestState"/> command
/// succeeds. Placed before <see cref="TransactionBehavior{TMessage,TResponse}"/> in the pipeline so
/// <c>next()</c> only returns once the DB transaction backing the command has already been committed
/// - the resync is requested against durable state, never against changes that could still roll back.
/// </summary>
public sealed class ManifestSyncTriggerBehavior<TMessage, TResponse>(IManifestSyncTrigger syncTrigger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken ct)
    {
        var response = await next(message, ct);

        if (message is IMutatesManifestState && response is Result { IsSuccess: true })
            syncTrigger.RequestSync();

        return response;
    }
}
