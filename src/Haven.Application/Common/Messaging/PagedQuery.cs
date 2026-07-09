namespace Haven.Application.Common.Messaging;

public abstract class PagedQuery<TResponse> : Mediator.IQuery<PagedResult<TResponse>>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}