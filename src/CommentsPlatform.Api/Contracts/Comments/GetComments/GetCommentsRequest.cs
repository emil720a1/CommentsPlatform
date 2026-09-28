namespace CommentsPlatform.Api.Contracts.Comments.GetComments;

public sealed record GetCommentsRequest(
    int Page = 1,
    int PageSize = 25,
    CommentSortField SortBy = CommentSortField.CreatedAt,
    CommentSortOrder SortDirection = CommentSortOrder.Descending);

public enum CommentSortField
{
    CreatedAt = 0,
    UserName = 1,
    Email = 2
}

public enum CommentSortOrder
{
    Ascending,
    Descending
}
