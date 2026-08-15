namespace SbaCars.BuildingBlocks.Application;

/// <summary>
/// A page/page-size request, self-clamping to sane bounds so that callers never need to
/// re-validate it before using it in a query.
/// </summary>
public sealed class PagedRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>
    /// The requested page, 1-based. Values below 1 are clamped to 1.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// The requested page size. Values below 1 fall back to <see cref="DefaultPageSize"/>;
    /// values above <see cref="MaxPageSize"/> are clamped to it.
    /// </summary>
    public int PageSize { get; }

    public PagedRequest(int page = 1, int pageSize = DefaultPageSize)
    {
        Page = page < 1 ? 1 : page;
        PageSize = pageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize,
        };
    }

    /// <summary>
    /// The number of records to skip to reach <see cref="Page"/>, for use in
    /// <c>Skip</c>/<c>Take</c> queries.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}
