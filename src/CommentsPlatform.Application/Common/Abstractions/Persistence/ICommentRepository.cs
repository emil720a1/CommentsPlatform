using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using CommentsPlatform.Domain;

namespace CommentsPlatform.Application.Common.Abstractions.Persistence;

public interface ICommentRepository
{
    Task<bool> ExistsAsync(Guid commentId, CancellationToken cancellationToken);

    Task<Comment?> GetByIdAsync(
        Guid commentId,
        CancellationToken cancellationToken);

    Task AddAsync(Comment comment, CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);

    Task<PaginatedList<CommentDto>> GetTopLevelCommentsAsync(
        GetCommentsParameters parameters,
        CancellationToken cancellationToken);
}
