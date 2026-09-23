namespace CommentsPlatform.Api.Contracts.Comments.GetComments;

public sealed record CommentResponse(
    Guid Id,
    string UserName,
    string? HomePage,
    DateTimeOffset CreatedAt,
    string Message);
