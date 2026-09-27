using CommentsPlatform.Api.Contracts.Comments;
using CommentsPlatform.Api.Contracts.Comments.Attachments;
using CommentsPlatform.Api.Contracts.Comments.GetComments;
using CommentsPlatform.Api.Contracts.Common;
using CommentsPlatform.Api.Controllers;
using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Attachments.Upload;
using CommentsPlatform.Application.Features.Attachments.Download;
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
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var expectedCommentId = Guid.NewGuid();
        ErrorOr<Guid> expectedResult = expectedCommentId;

        _senderMock.Setup(_senderMock => _senderMock.Send(
            It.IsAny<CreateCommentCommand>(),
            It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

        var result = await _controller.CreateComment(
            request,
            cancellationToken);

        var objectResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var response = Assert.IsType<CreateCommentResponse>(objectResult.Value);

        Assert.Equal(expectedCommentId, response.Id);

        _senderMock.Verify(
            sender => sender.Send(
                It.Is<CreateCommentCommand>(command =>
                    command.UserName == request.UserName &&
                    command.Email == request.Email &&
                    command.HomePage == request.HomePage &&
                    command.Message == request.Message &&
                    command.ParentCommentId == request.ParentCommentId &&
                    command.CaptchaToken == request.CaptchaToken),
                cancellationToken),
            Times.Once);
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

        var result = await _controller.CreateComment(request, CancellationToken.None);

        var returnedErrors = AssertProblemDetails(
            result,
            StatusCodes.Status400BadRequest,
            "Validation error",
            "One or more validation errors occurred.");
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

        var result = await _controller.CreateComment(request, CancellationToken.None);

        var returnedErrors = AssertProblemDetails(
            result,
            StatusCodes.Status404NotFound,
            "Resource not found",
            expectedError.Description);
        var error = Assert.Single(returnedErrors);

        Assert.Equal(expectedError.Code, error.Code);
        Assert.Equal(expectedError.Description, error.Description);
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

        var result = await _controller.CreateComment(request, CancellationToken.None);

        var returnedErrors = AssertProblemDetails(
            result,
            StatusCodes.Status500InternalServerError,
            "Internal server error",
            "An unexpected error occurred.");
        var error = Assert.Single(returnedErrors);

        Assert.Equal("Server.Unexpected", error.Code);
        Assert.Equal("An unexpected error occurred.", error.Description);
    }

    [Fact]
    public async Task UploadAttachment_WhenCommandSucceeds_ReturnsCreatedResultAndMapsRequest()
    {
        byte[] fileContent = [1, 2, 3, 4];
        await using var stream = new MemoryStream(fileContent);
        var request = CreateUploadAttachmentRequest(
            stream,
            "image.png",
            "image/png");
        var file = Assert.IsAssignableFrom<IFormFile>(request.File);
        var commentId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        ErrorOr<Guid> expectedResult = attachmentId;
        UploadAttachmentCommand? capturedCommand = null;
        var contentWasReadableDuringSend = false;

        _senderMock
            .Setup(sender => sender.Send(
                It.IsAny<UploadAttachmentCommand>(),
                cancellationToken))
            .Callback((IRequest<ErrorOr<Guid>> requestMessage, CancellationToken _) =>
            {
                capturedCommand = Assert.IsType<UploadAttachmentCommand>(
                    requestMessage);
                contentWasReadableDuringSend = capturedCommand.Content.CanRead;
            })
            .ReturnsAsync(expectedResult);

        var result = await _controller.UploadAttachment(
            commentId,
            request,
            cancellationToken);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var response = Assert.IsType<UploadAttachmentResponse>(
            objectResult.Value);
        Assert.Equal(attachmentId, response.AttachmentId);
        Assert.NotNull(capturedCommand);
        Assert.True(contentWasReadableDuringSend);
        Assert.Equal(commentId, capturedCommand.CommentId);
        Assert.Equal(file.FileName, capturedCommand.OriginalFileName);
        Assert.Equal(file.ContentType, capturedCommand.ContentType);
        Assert.Equal(file.Length, capturedCommand.FileSizeBytes);

        _senderMock.Verify(
            sender => sender.Send(
                It.IsAny<UploadAttachmentCommand>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task UploadAttachment_WhenValidationFails_ReturnsBadRequest()
    {
        await using var stream = new MemoryStream([1]);
        var request = CreateUploadAttachmentRequest(
            stream,
            "malware.exe",
            "application/executable");
        var expectedError = Error.Validation(
            "Attachments.ContentType.Unsupported",
            "The attachment content type is not supported.");

        _senderMock
            .Setup(sender => sender.Send(
                It.IsAny<UploadAttachmentCommand>(),
                CancellationToken.None))
            .ReturnsAsync(expectedError);

        var result = await _controller.UploadAttachment(
            Guid.NewGuid(),
            request,
            CancellationToken.None);

        var returnedErrors = AssertProblemDetails(
            result,
            StatusCodes.Status400BadRequest,
            "Validation error",
            "One or more validation errors occurred.");
        var error = Assert.Single(returnedErrors);

        Assert.Equal(expectedError.Code, error.Code);
        Assert.Equal(expectedError.Description, error.Description);
    }

    [Fact]
    public async Task UploadAttachment_WithoutFile_ReturnsBadRequestWithoutDispatchingCommand()
    {
        var request = new UploadAttachmentRequest();

        var result = await _controller.UploadAttachment(
            Guid.NewGuid(),
            request,
            CancellationToken.None);

        var returnedErrors = AssertProblemDetails(
            result,
            StatusCodes.Status400BadRequest,
            "Validation error",
            "One or more validation errors occurred.");
        var error = Assert.Single(returnedErrors);

        Assert.Equal("Attachments.File.Required", error.Code);
        Assert.Equal("The attachment file is required.", error.Description);

        _senderMock.Verify(
            sender => sender.Send(
                It.IsAny<UploadAttachmentCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadAttachment_WhenCommentDoesNotExist_ReturnsNotFound()
    {
        await using var stream = new MemoryStream([1]);
        var request = CreateUploadAttachmentRequest(
            stream,
            "image.png",
            "image/png");

        _senderMock
            .Setup(sender => sender.Send(
                It.IsAny<UploadAttachmentCommand>(),
                CancellationToken.None))
            .ReturnsAsync(UploadAttachmentErrors.CommentNotFound);

        var result = await _controller.UploadAttachment(
            Guid.NewGuid(),
            request,
            CancellationToken.None);

        var returnedErrors = AssertProblemDetails(
            result,
            StatusCodes.Status404NotFound,
            "Resource not found",
            UploadAttachmentErrors.CommentNotFound.Description);
        var error = Assert.Single(returnedErrors);

        Assert.Equal(UploadAttachmentErrors.CommentNotFound.Code, error.Code);
    }

    [Fact]
    public async Task UploadAttachment_WhenStorageFails_ReturnsSafeInternalServerError()
    {
        await using var stream = new MemoryStream([1]);
        var request = CreateUploadAttachmentRequest(
            stream,
            "image.png",
            "image/png");

        _senderMock
            .Setup(sender => sender.Send(
                It.IsAny<UploadAttachmentCommand>(),
                CancellationToken.None))
            .ReturnsAsync(UploadAttachmentErrors.StorageFailure);

        var result = await _controller.UploadAttachment(
            Guid.NewGuid(),
            request,
            CancellationToken.None);

        var returnedErrors = AssertProblemDetails(
            result,
            StatusCodes.Status500InternalServerError,
            "Internal server error",
            "An unexpected error occurred.");
        var error = Assert.Single(returnedErrors);

        Assert.Equal("Server.Unexpected", error.Code);
        Assert.Equal("An unexpected error occurred.", error.Description);
    }

    [Fact]
    public async Task GetComments_WhenQuerySucceeds_ReturnsOkWithMappedResponse()
    {
        var request = new GetCommentsRequest(
            Page: 2,
            PageSize: 10,
            SortBy: CommentSortField.CreatedAt,
            SortDirection: CommentSortOrder.Ascending);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var commentId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var attachmentId = Guid.NewGuid();
        var replyId = Guid.NewGuid();
        var comment = new CommentDto(
            commentId,
            "user1",
            "https://example.com/",
            createdAt,
            "private@example.com",
            "Test message",
            new[]
            {
                new AttachmentDto(
                    attachmentId,
                    "photo.png",
                    "image/png",
                    1024,
                    640,
                    480,
                    createdAt)
            })
        {
            Replies =
            [
                new CommentDto(
                    replyId,
                    "reply1",
                    null,
                    createdAt.AddMinutes(1),
                    "reply@example.com",
                    "Reply message",
                    Array.Empty<AttachmentDto>())
            ]
        };
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
                    query.SortBy == CommentSortBy.CreatedAt &&
                    query.SortDirection == SortDirection.Ascending),
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
        Assert.Equal(comment.Email, responseComment.Email);
        Assert.Equal(comment.HomePage, responseComment.HomePage);
        Assert.Equal(createdAt, responseComment.CreatedAt);
        Assert.Equal(comment.Message, responseComment.Message);
        var responseAttachment = Assert.Single(responseComment.Attachments);
        Assert.Equal(attachmentId, responseAttachment.Id);
        Assert.Equal("photo.png", responseAttachment.OriginalFileName);
        Assert.Equal("image/png", responseAttachment.ContentType);
        Assert.Equal(1024, responseAttachment.FileSizeBytes);
        Assert.Equal(
            $"/api/comments/{commentId}/attachments/{attachmentId}",
            responseAttachment.DownloadUrl);
        var responseReply = Assert.Single(responseComment.Replies);
        Assert.Equal(replyId, responseReply.Id);
        Assert.Equal("reply1", responseReply.UserName);
        Assert.Empty(responseReply.Replies);
        _senderMock.Verify(sender => sender.Send(
                It.Is<GetCommentsQuery>(query =>
                    query.Page == request.Page &&
                    query.PageSize == request.PageSize &&
                    query.SortBy == CommentSortBy.CreatedAt &&
                    query.SortDirection == SortDirection.Ascending),
                cancellationToken),
            Times.Once);
    }

    [Theory]
    [InlineData(CommentSortField.UserName, CommentSortOrder.Ascending, CommentSortBy.UserName, SortDirection.Ascending)]
    [InlineData(CommentSortField.UserName, CommentSortOrder.Descending, CommentSortBy.UserName, SortDirection.Descending)]
    [InlineData(CommentSortField.Email, CommentSortOrder.Ascending, CommentSortBy.Email, SortDirection.Ascending)]
    [InlineData(CommentSortField.Email, CommentSortOrder.Descending, CommentSortBy.Email, SortDirection.Descending)]
    [InlineData(CommentSortField.CreatedAt, CommentSortOrder.Ascending, CommentSortBy.CreatedAt, SortDirection.Ascending)]
    [InlineData(CommentSortField.CreatedAt, CommentSortOrder.Descending, CommentSortBy.CreatedAt, SortDirection.Descending)]
    public async Task GetComments_WithSupportedSorting_DispatchesMappedQuery(
        CommentSortField requestSortBy,
        CommentSortOrder requestSortDirection,
        CommentSortBy expectedSortBy,
        SortDirection expectedSortDirection)
    {
        var request = new GetCommentsRequest(
            Page: 2,
            PageSize: 25,
            SortBy: requestSortBy,
            SortDirection: requestSortDirection);
        ErrorOr<PaginatedList<CommentDto>> result = new PaginatedList<CommentDto>(
            Array.Empty<CommentDto>(),
            page: 2,
            pageSize: 25,
            totalCount: 0);

        _senderMock
            .Setup(sender => sender.Send(
                It.IsAny<GetCommentsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        await _controller.GetComments(request, CancellationToken.None);

        _senderMock.Verify(sender => sender.Send(
                It.Is<GetCommentsQuery>(query =>
                    query.Page == 2 &&
                    query.PageSize == 25 &&
                    query.SortBy == expectedSortBy &&
                    query.SortDirection == expectedSortDirection),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task DownloadAttachment_WhenQuerySucceeds_ReturnsFileResult()
    {
        var commentId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var content = new MemoryStream([1, 2, 3]);
        ErrorOr<DownloadAttachmentResult> queryResult = new DownloadAttachmentResult(
            content,
            "image/png",
            "photo.png",
            3);
        _senderMock.Setup(sender => sender.Send(
                new DownloadAttachmentQuery(commentId, attachmentId),
                CancellationToken.None))
            .ReturnsAsync(queryResult);

        var result = await _controller.DownloadAttachment(
            commentId,
            attachmentId,
            inline: false,
            CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Same(content, fileResult.FileStream);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal("photo.png", fileResult.FileDownloadName);
        Assert.True(fileResult.EnableRangeProcessing);
    }

    [Fact]
    public async Task DownloadAttachment_WhenMissing_ReturnsNotFound()
    {
        _senderMock.Setup(sender => sender.Send(
                It.IsAny<DownloadAttachmentQuery>(),
                CancellationToken.None))
            .ReturnsAsync(DownloadAttachmentErrors.NotFound);

        var result = await _controller.DownloadAttachment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            inline: false,
            CancellationToken.None);

        var errors = AssertProblemDetails(
            result,
            StatusCodes.Status404NotFound,
            "Resource not found",
            "The attachment was not found.");
        Assert.Equal("Attachments.NotFound", Assert.Single(errors).Code);
    }

    [Fact]
    public async Task DownloadAttachment_WhenImageIsRequestedInline_ReturnsInlineFile()
    {
        var content = new MemoryStream([1]);
        _senderMock.Setup(sender => sender.Send(
                It.IsAny<DownloadAttachmentQuery>(),
                CancellationToken.None))
            .ReturnsAsync(new DownloadAttachmentResult(
                content,
                "image/png",
                "photo.png",
                1));

        var result = await _controller.DownloadAttachment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            inline: true,
            CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Empty(fileResult.FileDownloadName);
        Assert.True(fileResult.EnableRangeProcessing);
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

        var returnedErrors = AssertProblemDetails(
            result,
            StatusCodes.Status400BadRequest,
            "Validation error",
            "One or more validation errors occurred.");
        var returnedError = Assert.Single(returnedErrors);

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

        var returnedErrors = AssertProblemDetails(
            result,
            StatusCodes.Status500InternalServerError,
            "Internal server error",
            "An unexpected error occurred.");
        var error = Assert.Single(returnedErrors);

        Assert.Equal("Server.Unexpected", error.Code);
        Assert.Equal("An unexpected error occurred.", error.Description);
    }

    [Theory]
    [InlineData(
        999,
        (int)CommentSortOrder.Descending,
        "Comments.SortBy.Invalid")]
    [InlineData(
        (int)CommentSortField.CreatedAt,
        999,
        "Comments.SortDirection.Invalid")]
    public async Task GetComments_WithUnsupportedSorting_ReturnsBadRequestWithoutDispatchingQuery(
        int sortBy,
        int sortDirection,
        string expectedErrorCode)
    {
        var request = new GetCommentsRequest(
            SortBy: (CommentSortField)sortBy,
            SortDirection: (CommentSortOrder)sortDirection);

        var result = await _controller.GetComments(
            request,
            CancellationToken.None);

        var returnedErrors = AssertProblemDetails(
            result,
            StatusCodes.Status400BadRequest,
            "Validation error",
            "One or more validation errors occurred.");
        var error = Assert.Single(returnedErrors);

        Assert.Equal(expectedErrorCode, error.Code);

        _senderMock.Verify(sender => sender.Send(
                It.IsAny<GetCommentsQuery>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ApiError[] AssertProblemDetails(
        IActionResult result,
        int expectedStatusCode,
        string expectedTitle,
        string expectedDetail)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(expectedStatusCode, objectResult.StatusCode);

        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);

        Assert.Equal(expectedStatusCode, problemDetails.Status);
        Assert.Equal(expectedTitle, problemDetails.Title);
        Assert.Equal(expectedDetail, problemDetails.Detail);

        return Assert.IsType<ApiError[]>(
            problemDetails.Extensions["errors"]);
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
            null,
            "valid-captcha-token");
    }

    private static UploadAttachmentRequest CreateUploadAttachmentRequest(
        Stream content,
        string fileName,
        string contentType)
    {
        var file = new FormFile(
            content,
            baseStreamOffset: 0,
            length: content.Length,
            name: "File",
            fileName: fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

        return new UploadAttachmentRequest
        {
            File = file
        };
    }
}
