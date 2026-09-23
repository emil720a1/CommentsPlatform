using CommentsPlatform.Api.Contracts.Comments;
using CommentsPlatform.Api.Contracts.Comments.GetComments;
using CommentsPlatform.Application.Features.Comments.Create;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ApplicationCommentSortBy = CommentsPlatform.Application.Features.Comments.Queries.GetComments.CommentSortBy;
using ApplicationSortDirection = CommentsPlatform.Application.Features.Comments.Queries.GetComments.SortDirection;

namespace CommentsPlatform.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly ISender _sender;

    public CommentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest request)
    {
        var result = await _sender.Send(new CreateCommentCommand(request.UserName, request.Email, request.HomePage, request.Message, request.ParentCommentId));

        if (result.IsError)
        {
            switch (result.FirstError.Type)
            {
                case ErrorType.Validation:
                    return BadRequest(result.Errors);

                case ErrorType.NotFound:
                    return NotFound(result.FirstError.Description);

                default:
                    return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        return StatusCode(StatusCodes.Status201Created, new CreateCommentResponse(result.Value));
    }

    [HttpGet]
    public async Task<IActionResult> GetComments(
        [FromQuery] GetCommentsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryMapSortBy(request.SortBy, out var sortBy) ||
            !TryMapSortDirection(request.SortDirection, out var sortDirection))
        {
            return BadRequest();
        }

        var result = await _sender.Send(
            new GetCommentsQuery(
                request.Page,
                request.PageSize,
                sortBy,
                sortDirection),
            cancellationToken);

        if (result.IsError)
        {
            return result.FirstError.Type switch
            {
                ErrorType.Validation => BadRequest(result.Errors),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        var page = result.Value;

        var comments = page.Items
            .Select(comment => new CommentResponse(
                comment.Id,
                comment.UserName,
                comment.HomePage,
                comment.CreatedAt,
                comment.Message))
            .ToList();

        return Ok(new GetCommentsResponse(
            comments,
            page.Page,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            page.HasPreviousPage,
            page.HasNextPage));
    }

    private static bool TryMapSortBy(
        CommentSortField sortBy,
        out ApplicationCommentSortBy mappedSortBy)
    {
        switch (sortBy)
        {
            case CommentSortField.CreatedAt:
                mappedSortBy = ApplicationCommentSortBy.CreatedAt;
                return true;

            default:
                mappedSortBy = default;
                return false;
        }
    }

    private static bool TryMapSortDirection(
        CommentSortOrder sortDirection,
        out ApplicationSortDirection mappedSortDirection)
    {
        switch (sortDirection)
        {
            case CommentSortOrder.Ascending:
                mappedSortDirection = ApplicationSortDirection.Ascending;
                return true;

            case CommentSortOrder.Descending:
                mappedSortDirection = ApplicationSortDirection.Descending;
                return true;

            default:
                mappedSortDirection = default;
                return false;
        }
    }
}
