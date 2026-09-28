using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using CommentsPlatform.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CommentsPlatform.Infrastructure.UnitTests.Caching;

public sealed class MemoryCommentsQueryCacheTests
{
    [Fact]
    public async Task SetAsync_WhenEntryIsCached_GetAsyncReturnsSameResult()
    {
        // Arrange
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cache = CreateCache(memoryCache);
        var parameters = CreateParameters();
        var expectedResult = CreateResult("cached-user");

        // Act
        await cache.SetAsync(
            parameters,
            expectedResult,
            CancellationToken.None);

        var result = await cache.GetAsync(
            parameters,
            CancellationToken.None);

        // Assert
        Assert.Same(expectedResult, result);
    }

    [Theory]
    [InlineData(2, 25, CommentSortBy.CreatedAt, SortDirection.Descending)]
    [InlineData(1, 50, CommentSortBy.CreatedAt, SortDirection.Descending)]
    [InlineData(1, 25, CommentSortBy.UserName, SortDirection.Descending)]
    [InlineData(1, 25, CommentSortBy.CreatedAt, SortDirection.Ascending)]
    public async Task SetAsync_WithDifferentQueryParameters_UsesDifferentCacheKeys(
        int page,
        int pageSize,
        CommentSortBy sortBy,
        SortDirection sortDirection)
    {
        // Arrange
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cache = CreateCache(memoryCache);

        var originalParameters = CreateParameters();
        var differentParameters = new GetCommentsParameters(
            page,
            pageSize,
            sortBy,
            sortDirection);

        var originalResult = CreateResult("original-user");
        var differentResult = CreateResult("different-user");

        // Act
        await cache.SetAsync(
            originalParameters,
            originalResult,
            CancellationToken.None);

        await cache.SetAsync(
            differentParameters,
            differentResult,
            CancellationToken.None);

        var cachedOriginalResult = await cache.GetAsync(
            originalParameters,
            CancellationToken.None);

        var cachedDifferentResult = await cache.GetAsync(
            differentParameters,
            CancellationToken.None);

        // Assert
        Assert.Same(originalResult, cachedOriginalResult);
        Assert.Same(differentResult, cachedDifferentResult);
    }

    [Fact]
    public async Task InvalidateAsync_WhenMultipleEntriesExist_RemovesAllEntries()
    {
        // Arrange
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cache = CreateCache(memoryCache);

        var firstParameters = CreateParameters();
        var secondParameters = new GetCommentsParameters(
            Page: 2,
            PageSize: 10,
            SortBy: CommentSortBy.Email,
            SortDirection: SortDirection.Ascending);

        await cache.SetAsync(
            firstParameters,
            CreateResult("first-user"),
            CancellationToken.None);

        await cache.SetAsync(
            secondParameters,
            CreateResult("second-user"),
            CancellationToken.None);

        // Act
        await cache.InvalidateAsync(CancellationToken.None);

        var firstResult = await cache.GetAsync(
            firstParameters,
            CancellationToken.None);

        var secondResult = await cache.GetAsync(
            secondParameters,
            CancellationToken.None);

        // Assert
        Assert.Null(firstResult);
        Assert.Null(secondResult);
    }

    [Fact]
    public async Task SetAsync_AfterInvalidation_CachesNewResult()
    {
        // Arrange
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cache = CreateCache(memoryCache);
        var parameters = CreateParameters();

        await cache.SetAsync(
            parameters,
            CreateResult("old-user"),
            CancellationToken.None);

        await cache.InvalidateAsync(CancellationToken.None);

        var expectedResult = CreateResult("new-user");

        // Act
        await cache.SetAsync(
            parameters,
            expectedResult,
            CancellationToken.None);

        var result = await cache.GetAsync(
            parameters,
            CancellationToken.None);

        // Assert
        Assert.Same(expectedResult, result);
    }

    [Fact]
    public async Task GetAsync_AfterAbsoluteExpiration_ReturnsNull()
    {
        // Arrange
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cache = CreateCache(memoryCache, durationSeconds: 1);
        var parameters = CreateParameters();

        await cache.SetAsync(
            parameters,
            CreateResult("expired-user"),
            CancellationToken.None);

        // Act
        await Task.Delay(TimeSpan.FromMilliseconds(1_100));

        var result = await cache.GetAsync(
            parameters,
            CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    private static MemoryCommentsQueryCache CreateCache(
        IMemoryCache memoryCache,
        int durationSeconds = 60)
    {
        return new MemoryCommentsQueryCache(
            memoryCache,
            Options.Create(new CommentsCacheOptions
            {
                DurationSeconds = durationSeconds,
            }));
    }

    private static GetCommentsParameters CreateParameters()
    {
        return new GetCommentsParameters(
            Page: 1,
            PageSize: 25,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Descending);
    }

    private static PaginatedList<CommentDto> CreateResult(
        string userName)
    {
        var comments = new List<CommentDto>
        {
            new(
                Guid.NewGuid(),
                userName,
                null,
                DateTimeOffset.UtcNow,
                $"{userName}@example.com",
                "Message",
                Array.Empty<AttachmentDto>()),
        };

        return new PaginatedList<CommentDto>(
            comments,
            page: 1,
            pageSize: 25,
            totalCount: 1);
    }
}
