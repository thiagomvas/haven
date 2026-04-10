namespace Haven.Application.Common.Messaging;

public interface IPagedQuery<TResponse> : IQuery<PagedResult<TResponse>>
{
    int PageNumber { get; }
    int PageSize { get; }
}
