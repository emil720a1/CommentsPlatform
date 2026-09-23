using CommentsPlatform.Api.Contracts.Comments;
using CommentsPlatform.Api.Contracts.Comments.GetComments;
using CommentsPlatform.Api.Controllers;
using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Comments.Create;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

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

    [Fact]
    public async Task GetComments_WhenQuerySucceeds_ReturnsOkWithMappedResponse()
    {
        var request = new GetCommentsRequest(
            Page: 2,
            PageSize: 10,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Ascending);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var commentId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var comment = new CommentDto(
            commentId,
            "user1",
            "https://example.com/",
            createdAt,
            "private@example.com",
            "Test message");
        var page = new PaginatedList<CommentDto>(
            new[] { comment },
            page: 2,
            pageSize: 10,
            totalCount: 21);
        ErrorOr<PaginatedList<CommentDto>> expectedResult = page;

        _senderMock
            .Setup(sender => sender.Send(
                It.Is<GetCommentsQuery>(query =>
                    query.Page == request.Page &&
                    query.PageSize == request.PageSize &&
                    query.SortBy == request.SortBy &&
                    query.SortDirection == request.SortDirection),
                cancellationToken))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetComments(request, cancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetCommentsResponse>(okResult.Value);
        var responseComment = Assert.Single(response.Items);

        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(2, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(21, response.TotalCount);
        Assert.Equal(3, response.TotalPages);
        Assert.True(response.HasPreviousPage);
        Assert.True(response.HasNextPage);
        Assert.Equal(commentId, responseComment.Id);
        Assert.Equal(comment.UserName, responseComment.UserName);
        Assert.Equal(comment.HomePage, responseComment.HomePage);
        Assert.Equal(createdAt, responseComment.CreatedAt);
        Assert.Equal(comment.Message, responseComment.Message);
        Assert.Null(typeof(CommentResponse).GetProperty("Email"));

        _senderMock.Verify(sender => sender.Send(
                It.Is<GetCommentsQuery>(query =>
                    query.Page == request.Page &&
                    query.PageSize == request.PageSize &&
                    query.SortBy == request.SortBy &&
                    query.SortDirection == request.SortDirection),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetComments_WhenValidationFails_ReturnsBadRequest()
    {
        var request = new GetCommentsRequest(Page: 0);
        var expectedError = Error.Validation(
            "Comments.Page.Invalid",
            "Page must be greater than zero.");

        _senderMock
            .Setup(sender => sender.Send(
                It.IsAny<GetCommentsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedError);

        var result = await _controller.GetComments(
            request,
            CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var returnedErrors = Assert.IsType<List<Error>>(badRequestResult.Value);
        var returnedError = Assert.Single(returnedErrors);

        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        Assert.Equal(expectedError.Code, returnedError.Code);
        Assert.Equal(expectedError.Description, returnedError.Description);
    }

    [Fact]
    public async Task GetComments_WhenUnexpectedErrorOccurs_ReturnsInternalServerError()
    {
        var request = new GetCommentsRequest();
        var unexpectedError = Error.Unexpected("Code", "Description");

        _senderMock
            .Setup(sender => sender.Send(
                It.IsAny<GetCommentsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(unexpectedError);

        var result = await _controller.GetComments(
            request,
            CancellationToken.None);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetComments_WithDefaultRequest_SendsQueryWithDefaultParameters()
    {
        var request = new GetCommentsRequest();
        var page = new PaginatedList<CommentDto>(
            Array.Empty<CommentDto>(),
            page: 1,
            pageSize: 25,
            totalCount: 0);
        ErrorOr<PaginatedList<CommentDto>> expectedResult = page;

        _senderMock
            .Setup(sender => sender.Send(
                It.IsAny<GetCommentsQuery>(),
                CancellationToken.None))
            .ReturnsAsync(expectedResult);

        var result = await _controller.GetComments(
            request,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<GetCommentsResponse>(okResult.Value);

        Assert.Empty(response.Items);
        Assert.Equal(1, response.Page);
        Assert.Equal(25, response.PageSize);
        Assert.Equal(0, response.TotalCount);
        Assert.Equal(0, response.TotalPages);

        _senderMock.Verify(sender => sender.Send(
                It.Is<GetCommentsQuery>(query =>
                    query.Page == 1 &&
                    query.PageSize == 25 &&
                    query.SortBy == CommentSortBy.CreatedAt &&
                    query.SortDirection == SortDirection.Descending),
                CancellationToken.None),
            Times.Once);
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
