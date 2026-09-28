using ErrorOr;

namespace CommentsPlatform.Application.Features.Attachments.Download;

public static class DownloadAttachmentErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: "Attachments.NotFound",
        description: "The attachment was not found.");

    public static readonly Error StorageFailure = Error.Failure(
        code: "Attachments.StorageFailure",
        description: "The attachment could not be read.");
}
