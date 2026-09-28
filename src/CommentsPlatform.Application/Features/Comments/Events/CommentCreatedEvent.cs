using CommentsPlatform.Application.Common.Events;

namespace CommentsPlatform.Application.Features.Comments.Events;

public sealed record CommentCreatedEvent(
    Guid CommentId) : IApplicationEvent;
