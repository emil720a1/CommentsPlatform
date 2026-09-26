using ErrorOr;

namespace CommentsPlatform.Application.Features.Attachments.Upload;

public static class UploadAttachmentErrors
{
    public static readonly Error CommentNotFound = Error.NotFound(
        "Attachments.CommentNotFound",
        "The comment was not found.");

    public static readonly Error StorageFailure = Error.Failure(
        "Attachments.StorageFailure",
        "The attachment could not be stored.");

    public static readonly Error PersistenceFailure = Error.Failure(
        "Attachments.PersistenceFailure",
        "The attachment metadata could not be saved.");
}