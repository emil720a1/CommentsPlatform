using System.ComponentModel.DataAnnotations;

namespace CommentsPlatform.Api.Contracts.Comments.Attachments;

public sealed class UploadAttachmentRequest
{
    [Required]
    public IFormFile? File { get; init; }
}
