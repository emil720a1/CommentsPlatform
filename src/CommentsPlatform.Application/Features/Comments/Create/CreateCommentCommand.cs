using ErrorOr;
using MediatR;

namespace CommentsPlatform.Application.Features.Comments.Create;

public sealed record CreateCommentCommand(
    string UserName,
    string Email,
    string? HomePage,
    string Message,
    Guid? ParentCommentId) : IRequest<ErrorOr<Guid>>;
