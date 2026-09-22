namespace CommentsPlatform.Application.Features.Comments.Queries.GetComments;

public record CommentDto(
Guid Id,
string UserName,
string? HomePage,
DateTimeOffset CreatedAt,
string Email,
string Message);