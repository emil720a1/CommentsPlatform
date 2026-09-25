using ErrorOr;
using MediatR;

namespace CommentsPlatform.Application.Features.Attachments.Upload;

public sealed record UploadAttachmentCommand(
    Guid CommentId,
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes)
    : IRequest<ErrorOr<Guid>>;