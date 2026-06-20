using Mediator;

namespace Haven.Application.Common.Messaging;

public interface IPagedQueryHandler<TQuery, TResponse>
    : Mediator.IQueryHandler<TQuery, PagedResult<TResponse>>
    where TQuery : PagedQuery<TResponse>;