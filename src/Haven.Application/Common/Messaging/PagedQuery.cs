namespace Haven.Application.Common.Messaging;

public abstract class PagedQuery<TResponse> : Mediator.IQuery<PagedResult<TResponse>>
{
    private int PageNumber { get; set; }
    private int PageSize { get; set; }
}