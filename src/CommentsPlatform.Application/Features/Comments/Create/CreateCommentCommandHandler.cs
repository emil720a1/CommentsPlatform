using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Domain;
using ErrorOr;
using MediatR;

namespace CommentsPlatform.Application.Features.Comments.Create;

public sealed class CreateCommentCommandHandler
    : IRequestHandler<CreateCommentCommand, ErrorOr<Guid>>
{
    private readonly ICommentRepository _commentRepository;

    public CreateCommentCommandHandler(
        ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<ErrorOr<Guid>> Handle(
        CreateCommentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await _commentRepository.ExistsAsync(
                request.ParentCommentId.Value,
                cancellationToken);

            if (!parentExists)
            {
                return CreateCommentErrors.ParentNotFound;
            }
        }

        Comment comment;
        try
        {
            comment = Comment.Create(
                request.UserName,
                request.Email,
                request.HomePage,
                request.Message,
                request.ParentCommentId);
        }
        catch (ArgumentException exception)
        {
            return CreateCommentErrors.DomainValidation(exception.Message);
        }

        await _commentRepository.AddAsync(comment, cancellationToken);

        return comment.Id;
    }
}
