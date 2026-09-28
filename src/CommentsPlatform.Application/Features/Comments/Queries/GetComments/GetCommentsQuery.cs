using CommentsPlatform.Application.Common.Models;
using ErrorOr;
using MediatR;

namespace CommentsPlatform.Application.Features.Comments.Queries.GetComments;

public enum CommentSortBy
{
    CreatedAt = 0,
    UserName = 1,
    Email = 2
}

public enum SortDirection
{
    Ascending,
    Descending
}

public record GetCommentsQuery(
    int Page = 1,
    int PageSize = 25,
    CommentSortBy SortBy = CommentSortBy.CreatedAt,
    SortDirection SortDirection = SortDirection.Descending)
    : IRequest<ErrorOr<PaginatedList<CommentDto>>>;
