using ErrorOr;

namespace CommentsPlatform.Application.Features.Comments.Create;

public static class CreateCommentErrors
{
    public static Error ParentNotFound => Error.NotFound(
        "Comments.ParentNotFound",
        "The parent comment was not found.");

    public static Error InvalidCaptcha => Error.Validation(
        "Comments.Captcha.Invalid",
        "CAPTCHA validation failed.");

    public static Error DomainValidation(string message)
    {
        return Error.Validation(
            "Comments.DomainValidation",
            message);
    }
}
