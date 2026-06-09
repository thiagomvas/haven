using Mediator;

namespace Haven.Application.Common.Messaging;

public interface IQuery<TResponse> : Mediator.IQuery<Result<TResponse>>;