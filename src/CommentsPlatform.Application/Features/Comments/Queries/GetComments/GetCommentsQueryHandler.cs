using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Common.Models;
using ErrorOr;
using MediatR;

namespace CommentsPlatform.Application.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQueryHandler
    : IRequestHandler<GetCommentsQuery,
        ErrorOr<PaginatedList<CommentDto>>>
{
    private readonly ICommentRepository _commentRepository;

    public GetCommentsQueryHandler(ICommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<ErrorOr<PaginatedList<CommentDto>>> Handle(
        GetCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = new GetCommentsParameters(
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDirection);

        var result = await _commentRepository.GetTopLevelCommentsAsync(parameters, cancellationToken);

        return result;
    }
}