using CommentsPlatform.Application.Common.Models;

namespace CommentsPlatform.Application.Features.Comments.Queries.GetComments;

public interface ICommentsQueryCache
{
    Task<PaginatedList<CommentDto>?> GetAsync(
        GetCommentsParameters parameters,
        CancellationToken cancellationToken);

    Task SetAsync(
        GetCommentsParameters parameters,
        PaginatedList<CommentDto> result,
        CancellationToken cancellationToken);

    Task InvalidateAsync(
        CancellationToken cancellationToken);
}