namespace CommentsPlatform.Api.Contracts.Comments;

public record CreateCommentRequest(
    string UserName,
    string Email,
    string? HomePage,
    string Message,
    Guid? ParentCommentId);