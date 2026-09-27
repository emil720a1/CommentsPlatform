using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Common.Abstractions.Storage;
using ErrorOr;
using MediatR;

namespace CommentsPlatform.Application.Features.Attachments.Download;

public sealed class DownloadAttachmentQueryHandler
    : IRequestHandler<DownloadAttachmentQuery, ErrorOr<DownloadAttachmentResult>>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IFileStorage _fileStorage;

    public DownloadAttachmentQueryHandler(
        ICommentRepository commentRepository,
        IFileStorage fileStorage)
    {
        _commentRepository = commentRepository;
        _fileStorage = fileStorage;
    }

    public async Task<ErrorOr<DownloadAttachmentResult>> Handle(
        DownloadAttachmentQuery request,
        CancellationToken cancellationToken)
    {
        var attachment = await _commentRepository.GetAttachmentAsync(
            request.CommentId,
            request.AttachmentId,
            cancellationToken);

        if (attachment is null)
        {
            return DownloadAttachmentErrors.NotFound;
        }

        try
        {
            var content = await _fileStorage.OpenReadAsync(
                attachment.StorageKey,
                cancellationToken);

            return new DownloadAttachmentResult(
                content,
                attachment.ContentType,
                attachment.OriginalFileName,
                attachment.FileSizeBytes);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            return DownloadAttachmentErrors.NotFound;
        }
        catch (DirectoryNotFoundException)
        {
            return DownloadAttachmentErrors.NotFound;
        }
        catch
        {
            return DownloadAttachmentErrors.StorageFailure;
        }
    }
}
