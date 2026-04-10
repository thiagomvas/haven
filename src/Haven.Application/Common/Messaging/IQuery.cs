using Mediator;

namespace Haven.Application.Common.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
