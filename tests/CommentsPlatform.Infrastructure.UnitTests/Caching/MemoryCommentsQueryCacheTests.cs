using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using CommentsPlatform.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
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
            0,
            CancellationToken.None);

        var result = await cache.GetAsync(
            parameters,
            CancellationToken.None);

        // Assert
        Assert.Same(expectedResult, result.Result);
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
            0,
            CancellationToken.None);

        await cache.SetAsync(
            differentParameters,
            differentResult,
            0,
            CancellationToken.None);

        var cachedOriginalResult = await cache.GetAsync(
            originalParameters,
            CancellationToken.None);

        var cachedDifferentResult = await cache.GetAsync(
            differentParameters,
            CancellationToken.None);

        // Assert
        Assert.Same(originalResult, cachedOriginalResult.Result);
        Assert.Same(differentResult, cachedDifferentResult.Result);
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
            0,
            CancellationToken.None);

        await cache.SetAsync(
            secondParameters,
            CreateResult("second-user"),
            0,
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
        Assert.Null(firstResult.Result);
        Assert.Null(secondResult.Result);
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
            0,
            CancellationToken.None);

        await cache.InvalidateAsync(CancellationToken.None);

        var expectedResult = CreateResult("new-user");

        // Act
        await cache.SetAsync(
            parameters,
            expectedResult,
            1,
            CancellationToken.None);

        var result = await cache.GetAsync(
            parameters,
            CancellationToken.None);

        // Assert
        Assert.Same(expectedResult, result.Result);
    }

    [Fact]
    public async Task SetAsync_WithStaleVersion_DoesNotRepopulateInvalidatedCache()
    {
        // Arrange
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var cache = CreateCache(memoryCache);
        var parameters = CreateParameters();
        var staleSnapshot = await cache.GetAsync(
            parameters,
            CancellationToken.None);

        await cache.InvalidateAsync(CancellationToken.None);

        // Act
        await cache.SetAsync(
            parameters,
            CreateResult("stale-user"),
            staleSnapshot.Version,
            CancellationToken.None);

        var result = await cache.GetAsync(
            parameters,
            CancellationToken.None);

        // Assert
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task GetAsync_AfterAbsoluteExpiration_ReturnsNull()
    {
        // Arrange
        var clock = new TestSystemClock(DateTimeOffset.UtcNow);
        using var memoryCache = new MemoryCache(new MemoryCacheOptions
        {
            Clock = clock,
        });
        using var cache = CreateCache(memoryCache, durationSeconds: 1);
        var parameters = CreateParameters();

        await cache.SetAsync(
            parameters,
            CreateResult("expired-user"),
            0,
            CancellationToken.None);

        // Act
        clock.Advance(TimeSpan.FromSeconds(2));

        var result = await cache.GetAsync(
            parameters,
            CancellationToken.None);

        // Assert
        Assert.Null(result.Result);
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

    private sealed class TestSystemClock(
        DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration)
        {
            UtcNow = UtcNow.Add(duration);
        }
    }
}
