using Mediator;

namespace Haven.Application.Common.Messaging;

public interface IPagedQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<PagedResult<TResponse>>>
    where TQuery : IPagedQuery<TResponse>;
