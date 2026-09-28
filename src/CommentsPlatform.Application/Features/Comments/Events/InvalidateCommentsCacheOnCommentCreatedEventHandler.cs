using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using MediatR;

namespace CommentsPlatform.Application.Features.Comments.Events;

public sealed class InvalidateCommentsCacheOnCommentCreatedEventHandler
    : INotificationHandler<CommentCreatedEvent>
{
    private readonly ICommentsQueryCache _commentsQueryCache;

    public InvalidateCommentsCacheOnCommentCreatedEventHandler(
        ICommentsQueryCache commentsQueryCache)
    {
        _commentsQueryCache = commentsQueryCache;
    }

    public Task Handle(
        CommentCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        return _commentsQueryCache.InvalidateAsync(cancellationToken);
    }
}
