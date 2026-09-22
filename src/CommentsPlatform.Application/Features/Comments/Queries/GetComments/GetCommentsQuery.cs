using MediatR;
using ErrorOr;
using CommentsPlatform.Application.Common.Models;

namespace
CommentsPlatform.Application.Features.Comments.Queries.GetComments;

public enum CommentSortBy
{
    CreatedAt
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
    SortDirection SortDirection = SortDirection.Descending) :
IRequest<ErrorOr<PaginatedList<CommentDto>>>;