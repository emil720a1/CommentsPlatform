namespace CommentsPlatform.Api.Contracts.Comments.GetComments;

public sealed record CommentResponse(
    Guid Id,
    string UserName,
    string? HomePage,
    DateTimeOffset CreatedAt,
    string Message,
    IReadOnlyList<AttachmentResponse> Attachments);

public sealed record AttachmentResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    int? Width,
    int? Height,
    DateTimeOffset CreatedAt,
    string DownloadUrl);
