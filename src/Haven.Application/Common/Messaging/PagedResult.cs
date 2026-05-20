namespace Haven.Application.Common.Messaging;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    
    public PagedResult<TOther> Project<TOther>(Func<T, TOther> projector)
    {
        var projectedItems = Items.Select(projector).ToList();
        return new PagedResult<TOther>(projectedItems, TotalCount, PageNumber, PageSize);
    }
}
