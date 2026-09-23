using CommentsPlatform.Application.Features.Comments.Queries.GetComments;

namespace CommentsPlatform.Api.Contracts.Comments.GetComments;

public sealed record GetCommentsRequest(
    int Page = 1,
    int PageSize = 25,
    CommentSortBy SortBy = CommentSortBy.CreatedAt,
    SortDirection SortDirection = SortDirection.Descending);
