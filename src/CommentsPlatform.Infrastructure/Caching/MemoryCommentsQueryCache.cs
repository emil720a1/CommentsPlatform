using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace CommentsPlatform.Infrastructure.Caching;

public sealed class MemoryCommentsQueryCache
    : ICommentsQueryCache, IDisposable
{
    private const string CacheKeyPrefix = "comments:list";

    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan _duration;
    private readonly object _invalidationLock = new();

    private CancellationTokenSource _invalidationTokenSource = new();

    public MemoryCommentsQueryCache(
        IMemoryCache memoryCache,
        IOptions<CommentsCacheOptions> options)
    {
        _memoryCache = memoryCache;
        _duration = TimeSpan.FromSeconds(
            options.Value.DurationSeconds);
    }

    public Task<PaginatedList<CommentDto>?> GetAsync(
        GetCommentsParameters parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = CreateCacheKey(parameters);

        var result = _memoryCache.Get<
            PaginatedList<CommentDto>>(cacheKey);

        return Task.FromResult(result);
    }

    public Task SetAsync(
        GetCommentsParameters parameters,
        PaginatedList<CommentDto> result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CancellationChangeToken invalidationToken;

        lock (_invalidationLock)
        {
            invalidationToken = new CancellationChangeToken(
                _invalidationTokenSource.Token);
        }

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_duration)
            .AddExpirationToken(invalidationToken);

        _memoryCache.Set(
            CreateCacheKey(parameters),
            result,
            cacheEntryOptions);

        return Task.CompletedTask;
    }

    public Task InvalidateAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CancellationTokenSource previousTokenSource;

        lock (_invalidationLock)
        {
            previousTokenSource = _invalidationTokenSource;
            _invalidationTokenSource = new CancellationTokenSource();
        }

        previousTokenSource.Cancel();
        previousTokenSource.Dispose();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        lock (_invalidationLock)
        {
            _invalidationTokenSource.Dispose();
        }
    }

    private static string CreateCacheKey(
        GetCommentsParameters parameters)
    {
        return FormattableString.Invariant(
            $"{CacheKeyPrefix}:page={parameters.Page}:size={parameters.PageSize}:sort={parameters.SortBy}:direction={parameters.SortDirection}");
    }
}
