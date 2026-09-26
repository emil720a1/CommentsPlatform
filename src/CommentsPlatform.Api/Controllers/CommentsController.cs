using CommentsPlatform.Api.Common.Errors;
using CommentsPlatform.Api.Contracts.Comments;
using CommentsPlatform.Api.Contracts.Comments.Attachments;
using CommentsPlatform.Api.Contracts.Comments.GetComments;
using CommentsPlatform.Application.Features.Attachments.Upload;
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
    public async Task<IActionResult> CreateComment(
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateCommentCommand(
                request.UserName,
                request.Email,
                request.HomePage,
                request.Message,
                request.ParentCommentId,
                request.CaptchaToken),
            cancellationToken);

        if (result.IsError)
        {
            return ApiErrorMapper.Map(result.Errors);
        }

        return StatusCode(StatusCodes.Status201Created, new CreateCommentResponse(result.Value));
    }

    [HttpPost("{commentId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [ProducesResponseType(
        typeof(UploadAttachmentResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadAttachment(
        Guid commentId,
        [FromForm] UploadAttachmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return ApiErrorMapper.Map(
                new[]
                {
                    Error.Validation(
                        code: "Attachments.File.Required",
                        description: "The attachment file is required.")
                });
        }

        var file = request.File;

        await using var content = file.OpenReadStream();

        var result = await _sender.Send(
            new UploadAttachmentCommand(
                commentId,
                content,
                file.FileName,
                file.ContentType,
                file.Length),
            cancellationToken);

        if (result.IsError)
        {
            return ApiErrorMapper.Map(result.Errors);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new UploadAttachmentResponse(result.Value));
    }

    [HttpGet]
    public async Task<IActionResult> GetComments(
        [FromQuery] GetCommentsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryMapSortBy(request.SortBy, out var sortBy))
        {
            return ApiErrorMapper.Map(
                new[]
                {
                    Error.Validation(
                        code: "Comments.SortBy.Invalid",
                        description: "Sort field is invalid.")
                });
        }

        if (!TryMapSortDirection(
                request.SortDirection,
                out var sortDirection))
        {
            return ApiErrorMapper.Map(
                new[]
                {
                    Error.Validation(
                        code: "Comments.SortDirection.Invalid",
                        description: "Sort direction is invalid.")
                });
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
            return ApiErrorMapper.Map(result.Errors);
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
