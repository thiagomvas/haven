using System.Reflection;
using FluentValidation;
using Mediator;

namespace Haven.Application.Common.Behaviors;

public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken ct)
    {
        var failures = validators
            .Select(v => v.Validate(message))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(message, ct);

        var errorMessage = string.Join("\n", failures.Select(f => f.ErrorMessage));
        var error = Error.Validation with { Message = errorMessage };
        return CreateFailure(error);
    }

    private static TResponse CreateFailure(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var method = responseType.GetMethod("Failure", BindingFlags.Static | BindingFlags.Public, [typeof(Error)]);
            return (TResponse)method!.Invoke(null, [error])!;
        }

        throw new InvalidOperationException(
            $"ValidationBehavior cannot create a failure response for type '{responseType.Name}'. " +
            $"Only Result and Result<T> are supported.");
    }
}