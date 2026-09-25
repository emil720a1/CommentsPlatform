using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Common.Abstractions.Storage;
using ErrorOr;
using MediatR;

namespace CommentsPlatform.Application.Features.Attachments.Upload;

public sealed class UploadAttachmentCommandHandler
    : IRequestHandler<UploadAttachmentCommand, ErrorOr<Guid>>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly TimeProvider _timeProvider;

    public UploadAttachmentCommandHandler(
        ICommentRepository commentRepository,
        IFileStorage fileStorage,
        TimeProvider timeProvider)
    {
        _commentRepository = commentRepository;
        _fileStorage = fileStorage;
        _timeProvider = timeProvider;
    }

    public async Task<ErrorOr<Guid>> Handle(
        UploadAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdAsync(
            request.CommentId,
            cancellationToken);

        if (comment is null)
        {
            return UploadAttachmentErrors.CommentNotFound;
        }

        var extension = GetExtension(request.ContentType);

        var storageKey =
            $"comments/{request.CommentId:N}/" +
            $"{Guid.NewGuid():N}{extension}";

        string savedStorageKey;

        try
        {
            savedStorageKey = await _fileStorage.SaveAsync(
                request.Content,
                storageKey,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return UploadAttachmentErrors.StorageFailure;
        }

        try
        {
            var attachment = comment.AddAttachment(
                request.OriginalFileName,
                savedStorageKey,
                request.ContentType,
                request.FileSizeBytes,
                _timeProvider.GetUtcNow());

            await _commentRepository.SaveChangesAsync(
                cancellationToken);

            return attachment.Id;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            await TryDeleteAsync(savedStorageKey);
            throw;
        }
        catch
        {
            await TryDeleteAsync(savedStorageKey);
            return UploadAttachmentErrors.PersistenceFailure;
        }
    }

    private async Task TryDeleteAsync(string storageKey)
    {
        try
        {
            await _fileStorage.DeleteAsync(
                storageKey,
                CancellationToken.None);
        }
        catch
        {
            // Do not hide the original persistence failure.
        }
    }

    private static string GetExtension(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "text/plain" => ".txt",
            _ => string.Empty
        };
    }
}
