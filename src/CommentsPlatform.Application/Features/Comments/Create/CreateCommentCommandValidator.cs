using FluentValidation;

namespace CommentsPlatform.Application.Features.Comments.Create;

public sealed class CreateCommentCommandValidator
    : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(command => command.UserName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("User name is required.")
            .WithErrorCode("Comments.UserName.Required")
            .Matches("^[A-Za-z0-9]+$")
            .WithMessage("User name can contain only Latin letters and digits.")
            .WithErrorCode("Comments.UserName.InvalidCharacters");

        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Email is required.")
            .WithErrorCode("Comments.Email.Required")
            .EmailAddress()
            .WithMessage("Email format is invalid.")
            .WithErrorCode("Comments.Email.InvalidFormat");

        RuleFor(command => command.HomePage)
            .Must(BeValidHomePage)
            .WithMessage("Home page format is invalid.")
            .WithErrorCode("Comments.HomePage.InvalidFormat")
            .When(command =>
                !string.IsNullOrWhiteSpace(command.HomePage));

        RuleFor(command => command.Message)
            .NotEmpty()
            .WithMessage("Message is required.")
            .WithErrorCode("Comments.Message.Required");

        RuleFor(command => command.ParentCommentId)
            .NotEqual(Guid.Empty)
            .WithMessage("Parent comment id cannot be empty.")
            .WithErrorCode("Comments.ParentCommentId.Empty")
            .When(command => command.ParentCommentId.HasValue);

        RuleFor(command => command.CaptchaToken)
            .NotEmpty()
            .WithMessage("CAPTCHA token is required.")
            .WithErrorCode("Comments.Captcha.Required");
    }

    private static bool BeValidHomePage(string? homePage)
    {
        return Uri.TryCreate(
                   homePage,
                   UriKind.Absolute,
                   out var homePageUri) &&
               (homePageUri.Scheme == Uri.UriSchemeHttp ||
                homePageUri.Scheme == Uri.UriSchemeHttps);
    }
}
