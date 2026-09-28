namespace CommentsPlatform.Application.Features.Comments.Queries.GetComments;


public record GetCommentsParameters(
    int Page,
    int PageSize,
    CommentSortBy SortBy,
    SortDirection SortDirection);