using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Common.Abstractions.Storage;
using CommentsPlatform.Application.Features.Attachments.Download;
using CommentsPlatform.Domain;
using ErrorOr;
using Moq;

namespace CommentsPlatform.Application.UnitTests.Features.Attachments.Download;

public sealed class DownloadAttachmentQueryHandlerTests
{
    private readonly Mock<ICommentRepository> _repository = new();
    private readonly Mock<IFileStorage> _storage = new();

    [Fact]
    public async Task Handle_WhenAttachmentExists_ReturnsFileMetadataAndContent()
    {
        var comment = CreateComment();
        var attachment = comment.AddAttachment(
            "notes.txt",
            "comments/id/file.txt",
            "text/plain",
            3,
            DateTimeOffset.UtcNow);
        var stream = new MemoryStream([1, 2, 3]);
        var query = new DownloadAttachmentQuery(comment.Id, attachment.Id);

        _repository.Setup(repository => repository.GetAttachmentAsync(
                comment.Id,
                attachment.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);
        _storage.Setup(storage => storage.OpenReadAsync(
                attachment.StorageKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        var handler = new DownloadAttachmentQueryHandler(
            _repository.Object,
            _storage.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Same(stream, result.Value.Content);
        Assert.Equal("text/plain", result.Value.ContentType);
        Assert.Equal("notes.txt", result.Value.OriginalFileName);
        Assert.Equal(3, result.Value.FileSizeBytes);
    }

    [Fact]
    public async Task Handle_WhenAttachmentDoesNotExist_ReturnsNotFoundWithoutReadingStorage()
    {
        var query = new DownloadAttachmentQuery(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(repository => repository.GetAttachmentAsync(
                query.CommentId,
                query.AttachmentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Attachment?)null);
        var handler = new DownloadAttachmentQueryHandler(
            _repository.Object,
            _storage.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(DownloadAttachmentErrors.NotFound, result.FirstError);
        _storage.Verify(storage => storage.OpenReadAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStoredFileIsMissing_ReturnsNotFound()
    {
        var comment = CreateComment();
        var attachment = comment.AddAttachment(
            "notes.txt",
            "comments/id/file.txt",
            "text/plain",
            3,
            DateTimeOffset.UtcNow);
        _repository.Setup(repository => repository.GetAttachmentAsync(
                comment.Id,
                attachment.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);
        _storage.Setup(storage => storage.OpenReadAsync(
                attachment.StorageKey,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());
        var handler = new DownloadAttachmentQueryHandler(
            _repository.Object,
            _storage.Object);

        var result = await handler.Handle(
            new DownloadAttachmentQuery(comment.Id, attachment.Id),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(DownloadAttachmentErrors.NotFound, result.FirstError);
    }

    [Fact]
    public async Task Handle_WhenStorageReadFails_ReturnsStorageFailure()
    {
        var result = await HandleWithStorageExceptionAsync(
            new IOException("Simulated storage failure."));

        Assert.True(result.IsError);
        Assert.Equal(DownloadAttachmentErrors.StorageFailure, result.FirstError);
    }

    [Fact]
    public async Task Handle_WhenStorageAccessIsDenied_ReturnsStorageFailure()
    {
        var result = await HandleWithStorageExceptionAsync(
            new UnauthorizedAccessException("Simulated access failure."));

        Assert.True(result.IsError);
        Assert.Equal(DownloadAttachmentErrors.StorageFailure, result.FirstError);
    }

    private async Task<ErrorOr<DownloadAttachmentResult>>
        HandleWithStorageExceptionAsync(Exception exception)
    {
        var comment = CreateComment();
        var attachment = comment.AddAttachment(
            "notes.txt",
            "comments/id/file.txt",
            "text/plain",
            3,
            DateTimeOffset.UtcNow);
        _repository.Setup(repository => repository.GetAttachmentAsync(
                comment.Id,
                attachment.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);
        _storage.Setup(storage => storage.OpenReadAsync(
                attachment.StorageKey,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        var handler = new DownloadAttachmentQueryHandler(
            _repository.Object,
            _storage.Object);

        return await handler.Handle(
            new DownloadAttachmentQuery(comment.Id, attachment.Id),
            CancellationToken.None);
    }

    private static Comment CreateComment() => Comment.Create(
        "user1",
        "user@example.com",
        null,
        "Message",
        null,
        DateTimeOffset.UtcNow);
}
