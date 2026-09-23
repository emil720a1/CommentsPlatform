namespace CommentsPlatform.Api.Contracts.Comments.GetComments;

public sealed record GetCommentsResponse(
    IReadOnlyList<CommentResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
