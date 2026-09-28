using CommentsPlatform.Application.Features.Attachments.Events;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using Moq;
using Xunit;

namespace CommentsPlatform.Application.UnitTests.Features.Attachments.Events;

public sealed class InvalidateCommentsCacheOnAttachmentUploadedEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenAttachmentIsUploaded_InvalidatesCommentsCache()
    {
        // Arrange
        var commentsQueryCacheMock = new Mock<ICommentsQueryCache>();
        commentsQueryCacheMock
            .Setup(cache => cache.InvalidateAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler =
            new InvalidateCommentsCacheOnAttachmentUploadedEventHandler(
                commentsQueryCacheMock.Object);

        var notification = new AttachmentUploadedEvent(
            Guid.NewGuid(),
            Guid.NewGuid());

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act
        await handler.Handle(notification, cancellationToken);

        // Assert
        commentsQueryCacheMock.Verify(
            cache => cache.InvalidateAsync(cancellationToken),
            Times.Once);
    }
}
