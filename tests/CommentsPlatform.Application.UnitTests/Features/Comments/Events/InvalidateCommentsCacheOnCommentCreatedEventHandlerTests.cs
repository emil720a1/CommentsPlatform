using CommentsPlatform.Application.Features.Comments.Events;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using Moq;
using Xunit;

namespace CommentsPlatform.Application.UnitTests.Features.Comments.Events;

public sealed class InvalidateCommentsCacheOnCommentCreatedEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenCommentIsCreated_InvalidatesCommentsCache()
    {
        // Arrange
        var commentsQueryCacheMock = new Mock<ICommentsQueryCache>();
        commentsQueryCacheMock
            .Setup(cache => cache.InvalidateAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler =
            new InvalidateCommentsCacheOnCommentCreatedEventHandler(
                commentsQueryCacheMock.Object);

        var notification = new CommentCreatedEvent(Guid.NewGuid());
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
