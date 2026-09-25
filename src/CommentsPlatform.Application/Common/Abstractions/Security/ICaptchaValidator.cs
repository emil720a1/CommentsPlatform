namespace CommentsPlatform.Application.Common.Abstractions.Security;

public interface ICaptchaValidator
{
    Task<bool> IsValidAsync(
        string token,
        CancellationToken cancellationToken);
}