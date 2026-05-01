using Mediator;

namespace Haven.Application.Common.Messaging;

public interface IQueryHandler<TQuery, TResponse>
    : Mediator.IQueryHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;