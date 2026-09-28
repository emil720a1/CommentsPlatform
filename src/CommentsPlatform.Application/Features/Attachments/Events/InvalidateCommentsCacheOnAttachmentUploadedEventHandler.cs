using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using MediatR;

namespace CommentsPlatform.Application.Features.Attachments.Events;

public sealed class InvalidateCommentsCacheOnAttachmentUploadedEventHandler
    : INotificationHandler<AttachmentUploadedEvent>
{
    private readonly ICommentsQueryCache _commentsQueryCache;

    public InvalidateCommentsCacheOnAttachmentUploadedEventHandler(
        ICommentsQueryCache commentsQueryCache)
    {
        _commentsQueryCache = commentsQueryCache;
    }

    public Task Handle(
        AttachmentUploadedEvent notification,
        CancellationToken cancellationToken)
    {
        return _commentsQueryCache.InvalidateAsync(cancellationToken);
    }
}
