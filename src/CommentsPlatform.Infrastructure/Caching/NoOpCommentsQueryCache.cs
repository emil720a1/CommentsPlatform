using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;

namespace CommentsPlatform.Infrastructure.Caching;

public sealed class NoOpCommentsQueryCache : ICommentsQueryCache
{
    public Task<CommentsQueryCacheSnapshot> GetAsync(
        GetCommentsParameters parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new CommentsQueryCacheSnapshot(null, 0));
    }

    public Task SetAsync(
        GetCommentsParameters parameters,
        PaginatedList<CommentDto> result,
        long version,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public Task InvalidateAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}
