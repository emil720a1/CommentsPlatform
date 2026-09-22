using CommentsPlatform.Domain;

namespace CommentsPlatform.Application.Common.Abstractions.Persistence;

public interface ICommentRepository
{
    Task<bool> ExistsAsync(Guid commentId, CancellationToken cancellationToken);

    Task AddAsync(Comment comment, CancellationToken cancellationToken);
}
