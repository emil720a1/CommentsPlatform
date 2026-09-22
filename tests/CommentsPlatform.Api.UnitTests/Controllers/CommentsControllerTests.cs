using System.Runtime.InteropServices;
using CommentsPlatform.Api.Contracts.Comments;
using CommentsPlatform.Api.Controllers;
using CommentsPlatform.Application.Features.Comments.Create;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CommentsPlatform.Api.UnitTests.Controllers;

public class CommentsControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly CommentsController _controller;

    public CommentsControllerTests()
    {
        _senderMock = new Mock<ISender>();
        _controller = new CommentsController(_senderMock.Object);
    }

    [Fact]
    public async Task CreateComment_WhenCommandSucceeds_ReturnsCreatedResult()
    {
        var request = CreateValidRequest();

        var expectedCommentId = Guid.NewGuid();
        ErrorOr<Guid> expectedResult = expectedCommentId;

        _senderMock.Setup(_senderMock => _senderMock.Send(
            It.IsAny<CreateCommentCommand>(),
            It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

        var result = await _controller.CreateComment(request);

        var objectResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var response = Assert.IsType<CreateCommentResponse>(objectResult.Value);

        Assert.Equal(expectedCommentId, response.Id);
    }

    [Fact]
    public async Task CreateComment_WhenValidationFails_ReturnsBadRequest()
    {
        var request = CreateValidRequest();

        var expectedError = Error.Validation("Code", "Description");
        _senderMock.Setup(_senderMock => _senderMock.Send(
            It.IsAny<CreateCommentCommand>(),
            It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedError);

        var result = await _controller.CreateComment(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);

        var returnedErrors = Assert.IsType<List<Error>>(badRequestResult.Value);
        var error = Assert.Single(returnedErrors);
        Assert.Equal(expectedError.Code, error.Code);
        Assert.Equal(expectedError.Description, error.Description);
    }

    [Fact]
    public async Task CreateComment_WhenNotFound_ReturnsNotFound()
    {
        var request = CreateValidRequest();

        var expectedError = Error.NotFound("Code", "Description");
        _senderMock.Setup(_senderMock => _senderMock.Send(
            It.IsAny<CreateCommentCommand>(),
            It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedError);

        var result = await _controller.CreateComment(request);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);

        var errorMessage = Assert.IsType<string>(notFoundResult.Value);
        Assert.Equal(expectedError.Description, errorMessage);
    }

    [Fact]
    public async Task CreateComment_WhenUnexpectedErrorOccurs_ReturnsInternalServerError()
    {
        var request = CreateValidRequest();

        var unexpectedError = Error.Unexpected("Code", "Description");

        _senderMock.Setup(_senderMock => _senderMock.Send(
            It.IsAny<CreateCommentCommand>(),
            It.IsAny<CancellationToken>()))
                .ReturnsAsync(unexpectedError);

        var result = await _controller.CreateComment(request);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    private static CreateCommentRequest CreateValidRequest()
    {
        return new CreateCommentRequest(
            "user",
            "email@example.com",
            null,
            "message",
            null
        );
    }
}