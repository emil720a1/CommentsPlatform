using FluentValidation;

namespace CommentsPlatform.Application.Features.Attachments.Upload;

public sealed class UploadAttachmentCommandValidator
    : AbstractValidator<UploadAttachmentCommand>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "text/plain"
        };

    public UploadAttachmentCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(command => command.CommentId)
            .NotEmpty()
            .WithErrorCode("Attachments.CommentId.Required");

        RuleFor(command => command.Content)
            .NotNull()
            .WithErrorCode("Attachments.Content.Required")
            .Must(content => content is null || content.CanRead)
            .WithMessage("The attachment content must be readable.")
            .WithErrorCode("Attachments.Content.NotReadable");

        RuleFor(command => command.OriginalFileName)
            .NotEmpty()
            .WithErrorCode("Attachments.FileName.Required")
            .MaximumLength(255)
            .WithErrorCode("Attachments.FileName.TooLong")
            .Must(BeSafeFileName)
            .WithMessage("The attachment file name is invalid.")
            .WithErrorCode("Attachments.FileName.Invalid");

        RuleFor(command => command.ContentType)
            .NotEmpty()
            .WithErrorCode("Attachments.ContentType.Required")
            .Must(contentType => AllowedContentTypes.Contains(contentType))
            .WithMessage("The attachment content type is not supported.")
            .WithErrorCode("Attachments.ContentType.Unsupported");

        RuleFor(command => command.FileSizeBytes)
            .GreaterThan(0)
            .WithErrorCode("Attachments.FileSize.Empty")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("The attachment must not exceed 5 MiB.")
            .WithErrorCode("Attachments.FileSize.TooLarge");
    }

    private static bool BeSafeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return true;
        }

        return fileName == Path.GetFileName(fileName)
               && !fileName.Contains('/')
               && !fileName.Contains('\\')
               && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }
}