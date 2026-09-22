namespace CommentsPlatform.Application.Common.Models;

public sealed class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages { get; }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public PaginatedList(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount));
        }

        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;

        TotalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling((double)totalCount / pageSize);
    }
}
