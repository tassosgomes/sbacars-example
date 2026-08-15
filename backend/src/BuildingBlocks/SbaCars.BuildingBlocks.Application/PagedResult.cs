namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// A page of results, together with enough information for the caller to know whether more
/// pages exist without issuing another query.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; }

    /// <summary>
    /// The page these items belong to, 1-based.
    /// </summary>
    public int Page { get; }

    public int PageSize { get; }

    /// <summary>
    /// The total number of records across all pages, not just the current one.
    /// </summary>
    public long TotalCount { get; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    public bool HasPreviousPage => Page > 1;

    public PagedResult(IReadOnlyCollection<T> items, int page, int pageSize, long totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedResult<T> Empty(PagedRequest request) =>
        new([], request.Page, request.PageSize, 0);
}
