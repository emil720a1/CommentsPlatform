using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Common.Abstractions.Storage;
using CommentsPlatform.Application.Features.Attachments.Upload;
using CommentsPlatform.Domain;
using Moq;

namespace CommentsPlatform.Application.UnitTests.Features.Attachments.Upload;

public sealed class UploadAttachmentCommandHandlerTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICommentRepository> _commentRepositoryMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly UploadAttachmentCommandHandler _handler;

    public UploadAttachmentCommandHandlerTests()
    {
        _timeProviderMock
            .Setup(timeProvider => timeProvider.GetUtcNow())
            .Returns(FixedUtcNow);

        _handler = new UploadAttachmentCommandHandler(
            _commentRepositoryMock.Object,
            _fileStorageMock.Object,
            _timeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCommentDoesNotExist_ReturnsNotFoundAndDoesNotStoreFile()
    {
        await using var content = new MemoryStream([1, 2, 3]);
        var command = CreateCommand(Guid.NewGuid(), content);

        _commentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.CommentId,
                CancellationToken.None))
            .ReturnsAsync((Comment?)null);

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);
        var error = Assert.Single(result.Errors);
        Assert.Equal(UploadAttachmentErrors.CommentNotFound.Code, error.Code);
        Assert.Equal(UploadAttachmentErrors.CommentNotFound.Type, error.Type);

        _fileStorageMock.Verify(
            storage => storage.SaveAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _commentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _timeProviderMock.Verify(
            timeProvider => timeProvider.GetUtcNow(),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidCommand_StoresFileAndPersistsAttachment()
    {
        await using var content = new MemoryStream([1, 2, 3]);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var comment = CreateComment();
        var command = CreateCommand(comment.Id, content);
        string? capturedStorageKey = null;

        _commentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                comment.Id,
                cancellationToken))
            .ReturnsAsync(comment);

        _fileStorageMock
            .Setup(storage => storage.SaveAsync(
                content,
                It.IsAny<string>(),
                cancellationToken))
            .Callback<Stream, string, CancellationToken>(
                (_, storageKey, _) => capturedStorageKey = storageKey)
            .ReturnsAsync((Stream _, string storageKey, CancellationToken _) =>
                storageKey);

        _commentRepositoryMock
            .Setup(repository => repository.SaveChangesAsync(
                cancellationToken))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            command,
            cancellationToken);

        Assert.False(result.IsError);
        Assert.NotNull(capturedStorageKey);
        Assert.True(IsValidStorageKey(
            capturedStorageKey,
            comment.Id,
            ".png"));

        var attachment = Assert.Single(comment.Attachments);
        Assert.Equal(result.Value, attachment.Id);
        Assert.Equal(comment.Id, attachment.CommentId);
        Assert.Equal(command.OriginalFileName, attachment.OriginalFileName);
        Assert.Equal(capturedStorageKey, attachment.StorageKey);
        Assert.Equal(command.ContentType, attachment.ContentType);
        Assert.Equal(command.FileSizeBytes, attachment.FileSizeBytes);
        Assert.Equal(FixedUtcNow, attachment.CreatedAt);
        Assert.Null(attachment.Width);
        Assert.Null(attachment.Height);

        _commentRepositoryMock.Verify(
            repository => repository.GetByIdAsync(
                comment.Id,
                cancellationToken),
            Times.Once);

        _fileStorageMock.Verify(
            storage => storage.SaveAsync(
                content,
                capturedStorageKey,
                cancellationToken),
            Times.Once);

        _commentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                cancellationToken),
            Times.Once);

        _fileStorageMock.Verify(
            storage => storage.DeleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _timeProviderMock.Verify(
            timeProvider => timeProvider.GetUtcNow(),
            Times.Once);
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/gif", ".gif")]
    [InlineData("text/plain", ".txt")]
    public async Task Handle_WithSupportedContentType_GeneratesExpectedExtension(
        string contentType,
        string expectedExtension)
    {
        await using var content = new MemoryStream([1]);
        var comment = CreateComment();
        var command = CreateCommand(comment.Id, content) with
        {
            ContentType = contentType,
            FileSizeBytes = 1
        };

        _commentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                comment.Id,
                CancellationToken.None))
            .ReturnsAsync(comment);

        _fileStorageMock
            .Setup(storage => storage.SaveAsync(
                content,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync((Stream _, string storageKey, CancellationToken _) =>
                storageKey);

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.False(result.IsError);

        _fileStorageMock.Verify(
            storage => storage.SaveAsync(
                content,
                It.Is<string>(storageKey => IsValidStorageKey(
                    storageKey,
                    comment.Id,
                    expectedExtension)),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStorageFails_ReturnsStorageFailureAndDoesNotPersist()
    {
        await using var content = new MemoryStream([1, 2, 3]);
        var comment = CreateComment();
        var command = CreateCommand(comment.Id, content);

        _commentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                comment.Id,
                CancellationToken.None))
            .ReturnsAsync(comment);

        _fileStorageMock
            .Setup(storage => storage.SaveAsync(
                content,
                It.IsAny<string>(),
                CancellationToken.None))
            .ThrowsAsync(new IOException("Storage unavailable."));

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);
        var error = Assert.Single(result.Errors);
        Assert.Equal(UploadAttachmentErrors.StorageFailure.Code, error.Code);
        Assert.Empty(comment.Attachments);

        _commentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _fileStorageMock.Verify(
            storage => storage.DeleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _timeProviderMock.Verify(
            timeProvider => timeProvider.GetUtcNow(),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPersistenceFails_DeletesStoredFileAndReturnsPersistenceFailure()
    {
        await using var content = new MemoryStream([1, 2, 3]);
        var comment = CreateComment();
        var command = CreateCommand(comment.Id, content);
        const string savedStorageKey = "comments/comment-id/file.png";

        SetupExistingComment(comment);

        _fileStorageMock
            .Setup(storage => storage.SaveAsync(
                content,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(savedStorageKey);

        _commentRepositoryMock
            .Setup(repository => repository.SaveChangesAsync(
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Database unavailable."));

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);
        var error = Assert.Single(result.Errors);
        Assert.Equal(UploadAttachmentErrors.PersistenceFailure.Code, error.Code);

        _fileStorageMock.Verify(
            storage => storage.DeleteAsync(
                savedStorageKey,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDomainRejectsMetadata_DeletesStoredFileAndReturnsPersistenceFailure()
    {
        await using var content = new MemoryStream([1]);
        var comment = CreateComment();
        var command = CreateCommand(comment.Id, content) with
        {
            FileSizeBytes = 0
        };
        const string savedStorageKey = "comments/comment-id/file.png";

        SetupExistingComment(comment);

        _fileStorageMock
            .Setup(storage => storage.SaveAsync(
                content,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(savedStorageKey);

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);
        var error = Assert.Single(result.Errors);
        Assert.Equal(UploadAttachmentErrors.PersistenceFailure.Code, error.Code);
        Assert.Empty(comment.Attachments);

        _fileStorageMock.Verify(
            storage => storage.DeleteAsync(
                savedStorageKey,
                CancellationToken.None),
            Times.Once);

        _commentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStorageIsCancelled_PropagatesCancellation()
    {
        await using var content = new MemoryStream([1]);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var cancellationToken = cancellationTokenSource.Token;
        var comment = CreateComment();
        var command = CreateCommand(comment.Id, content);

        _commentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                comment.Id,
                cancellationToken))
            .ReturnsAsync(comment);

        _fileStorageMock
            .Setup(storage => storage.SaveAsync(
                content,
                It.IsAny<string>(),
                cancellationToken))
            .ThrowsAsync(new OperationCanceledException(cancellationToken));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.Handle(command, cancellationToken));

        _commentRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _fileStorageMock.Verify(
            storage => storage.DeleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPersistenceIsCancelled_DeletesStoredFileAndPropagatesCancellation()
    {
        await using var content = new MemoryStream([1]);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var cancellationToken = cancellationTokenSource.Token;
        var comment = CreateComment();
        var command = CreateCommand(comment.Id, content);
        const string savedStorageKey = "comments/comment-id/file.png";

        _commentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                comment.Id,
                cancellationToken))
            .ReturnsAsync(comment);

        _fileStorageMock
            .Setup(storage => storage.SaveAsync(
                content,
                It.IsAny<string>(),
                cancellationToken))
            .ReturnsAsync(savedStorageKey);

        _commentRepositoryMock
            .Setup(repository => repository.SaveChangesAsync(
                cancellationToken))
            .ThrowsAsync(new OperationCanceledException(cancellationToken));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.Handle(command, cancellationToken));

        _fileStorageMock.Verify(
            storage => storage.DeleteAsync(
                savedStorageKey,
                CancellationToken.None),
            Times.Once);
    }

    private void SetupExistingComment(Comment comment)
    {
        _commentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                comment.Id,
                CancellationToken.None))
            .ReturnsAsync(comment);
    }

    private static UploadAttachmentCommand CreateCommand(
        Guid commentId,
        Stream content)
    {
        return new UploadAttachmentCommand(
            commentId,
            content,
            "image.png",
            "image/png",
            3);
    }

    private static Comment CreateComment()
    {
        return Comment.Create(
            "user123",
            "user@example.com",
            null,
            "Test message",
            null,
            FixedUtcNow.AddMinutes(-1));
    }

    private static bool IsValidStorageKey(
        string storageKey,
        Guid commentId,
        string expectedExtension)
    {
        var expectedPrefix = $"comments/{commentId:N}/";

        if (!storageKey.StartsWith(
                expectedPrefix,
                StringComparison.Ordinal) ||
            !storageKey.EndsWith(
                expectedExtension,
                StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = storageKey[expectedPrefix.Length..^expectedExtension.Length];

        return Guid.TryParseExact(fileName, "N", out _);
    }
}
