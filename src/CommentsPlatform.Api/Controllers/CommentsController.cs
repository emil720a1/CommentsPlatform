using CommentsPlatform.Api.Contracts.Comments;
using CommentsPlatform.Application.Features.Comments.Create;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
}