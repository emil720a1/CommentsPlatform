using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Common.Abstractions.Storage;
using CommentsPlatform.Application.Features.Attachments.Events;
using ErrorOr;
using MediatR;

namespace CommentsPlatform.Application.Features.Attachments.Upload;

public sealed class UploadAttachmentCommandHandler
    : IRequestHandler<UploadAttachmentCommand, ErrorOr<Guid>>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly TimeProvider _timeProvider;
    private readonly IPublisher _publisher;

    public UploadAttachmentCommandHandler(
        ICommentRepository commentRepository,
        IFileStorage fileStorage,
        TimeProvider timeProvider,
        IPublisher publisher)
    {
        _commentRepository = commentRepository;
        _fileStorage = fileStorage;
        _timeProvider = timeProvider;
        _publisher = publisher;
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

        if (!cancellationToken.IsCancellationRequested &&
            IsImageContentType(request.ContentType) &&
            !await HasMatchingImageSignatureAsync(
                request.Content,
                request.ContentType,
                cancellationToken))
        {
            return UploadAttachmentErrors.InvalidContent;
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

        Guid attachmentId;

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

            attachmentId = attachment.Id;
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

        await _publisher.Publish(
            new AttachmentUploadedEvent(
                comment.Id,
                attachmentId),
            cancellationToken);

        return attachmentId;
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

    private static async Task<bool> HasMatchingImageSignatureAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            return false;
        }

        var originalPosition = content.Position;

        try
        {
            var header = new byte[8];
            var bytesRead = 0;

            while (bytesRead < header.Length)
            {
                var read = await content.ReadAsync(
                    header.AsMemory(bytesRead),
                    cancellationToken);

                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }

            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => bytesRead >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF,
                "image/png" => bytesRead >= 8 &&
                    StartsWith(
                        header,
                        bytesRead,
                        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
                "image/gif" => bytesRead >= 6 &&
                    (StartsWith(header, bytesRead, 0x47, 0x49, 0x46, 0x38, 0x37, 0x61) ||
                     StartsWith(header, bytesRead, 0x47, 0x49, 0x46, 0x38, 0x39, 0x61)),
                _ => true
            };
        }
        finally
        {
            content.Position = originalPosition;
        }
    }

    private static bool IsImageContentType(string contentType)
    {
        return contentType.StartsWith(
            "image/",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool StartsWith(
        byte[] content,
        int bytesRead,
        params byte[] signature)
    {
        if (bytesRead < signature.Length)
        {
            return false;
        }

        for (var index = 0; index < signature.Length; index++)
        {
            if (content[index] != signature[index])
            {
                return false;
            }
        }

        return true;
    }
}
