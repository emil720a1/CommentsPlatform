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
    private readonly ICommentsQueryCache _commentsQueryCache;

    public GetCommentsQueryHandler(
        ICommentRepository commentRepository,
        ICommentsQueryCache commentsQueryCache)
    {
        _commentRepository = commentRepository;
        _commentsQueryCache = commentsQueryCache;
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

        var cacheSnapshot = await _commentsQueryCache.GetAsync(
            parameters,
            cancellationToken);

        if (cacheSnapshot.Result is not null)
        {
            return cacheSnapshot.Result;
        }

        var result = await _commentRepository.GetTopLevelCommentsAsync(
            parameters,
            cancellationToken);

        await _commentsQueryCache.SetAsync(
            parameters,
            result,
            cacheSnapshot.Version,
            cancellationToken);

        return result;
    }
}
