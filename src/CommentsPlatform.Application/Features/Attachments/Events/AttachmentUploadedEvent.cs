using CommentsPlatform.Application.Common.Events;

namespace CommentsPlatform.Application.Features.Attachments.Events;

public sealed record AttachmentUploadedEvent(
    Guid CommentId,
    Guid AttachmentId) : IApplicationEvent;
