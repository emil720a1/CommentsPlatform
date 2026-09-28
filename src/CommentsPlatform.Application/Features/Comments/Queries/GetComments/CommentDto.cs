namespace CommentsPlatform.Application.Features.Comments.Queries.GetComments;

public record CommentDto(
    Guid Id,
    string UserName,
    string? HomePage,
    DateTimeOffset CreatedAt,
    string Email,
    string Message,
    IReadOnlyList<AttachmentDto> Attachments)
{
    public IReadOnlyList<CommentDto> Replies { get; init; } = [];
}

public sealed record AttachmentDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    int? Width,
    int? Height,
    DateTimeOffset CreatedAt);
