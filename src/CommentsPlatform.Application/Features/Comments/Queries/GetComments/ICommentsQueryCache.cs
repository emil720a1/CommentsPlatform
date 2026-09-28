using CommentsPlatform.Application.Common.Models;

namespace CommentsPlatform.Application.Features.Comments.Queries.GetComments;

public sealed record CommentsQueryCacheSnapshot(
    PaginatedList<CommentDto>? Result,
    long Version);

public interface ICommentsQueryCache
{
    Task<CommentsQueryCacheSnapshot> GetAsync(
        GetCommentsParameters parameters,
        CancellationToken cancellationToken);

    Task SetAsync(
        GetCommentsParameters parameters,
        PaginatedList<CommentDto> result,
        long version,
        CancellationToken cancellationToken);

    Task InvalidateAsync(
        CancellationToken cancellationToken);
}
