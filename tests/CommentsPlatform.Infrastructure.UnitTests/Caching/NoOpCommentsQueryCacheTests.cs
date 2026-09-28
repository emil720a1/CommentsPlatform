using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using CommentsPlatform.Infrastructure.Caching;

namespace CommentsPlatform.Infrastructure.UnitTests.Caching;

public sealed class NoOpCommentsQueryCacheTests
{
    private readonly NoOpCommentsQueryCache _cache = new();

    [Fact]
    public async Task GetAsync_Always_ReturnsNull()
    {
        var result = await _cache.GetAsync(
            CreateParameters(),
            CancellationToken.None);

        Assert.Null(result.Result);
    }

    [Fact]
    public async Task SetAsync_Always_DoesNotStoreResult()
    {
        var parameters = CreateParameters();

        await _cache.SetAsync(
            parameters,
            CreateResult(),
            0,
            CancellationToken.None);

        var result = await _cache.GetAsync(
            parameters,
            CancellationToken.None);

        Assert.Null(result.Result);
    }

    [Fact]
    public async Task InvalidateAsync_Always_CompletesSuccessfully()
    {
        var exception = await Record.ExceptionAsync(
            () => _cache.InvalidateAsync(CancellationToken.None));

        Assert.Null(exception);
    }

    private static GetCommentsParameters CreateParameters()
    {
        return new GetCommentsParameters(
            Page: 1,
            PageSize: 25,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Descending);
    }

    private static PaginatedList<CommentDto> CreateResult()
    {
        return new PaginatedList<CommentDto>(
            new List<CommentDto>(),
            page: 1,
            pageSize: 25,
            totalCount: 0);
    }
}
