using ErrorOr;
using MediatR;

namespace CommentsPlatform.Application.Features.Attachments.Download;

public sealed record DownloadAttachmentQuery(
    Guid CommentId,
    Guid AttachmentId) : IRequest<ErrorOr<DownloadAttachmentResult>>;

public sealed record DownloadAttachmentResult(
    Stream Content,
    string ContentType,
    string OriginalFileName,
    long FileSizeBytes);
