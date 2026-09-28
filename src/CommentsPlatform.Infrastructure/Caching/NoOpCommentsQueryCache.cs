using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;

namespace CommentsPlatform.Infrastructure.Caching;

public sealed class NoOpCommentsQueryCache : ICommentsQueryCache
{
    public Task<PaginatedList<CommentDto>?> GetAsync(
        GetCommentsParameters parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<PaginatedList<CommentDto>?>(null);
    }

    public Task SetAsync(
        GetCommentsParameters parameters,
        PaginatedList<CommentDto> result,
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
