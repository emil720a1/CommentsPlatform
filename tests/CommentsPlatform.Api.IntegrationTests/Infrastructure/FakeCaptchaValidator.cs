using CommentsPlatform.Application.Common.Abstractions.Security;

namespace CommentsPlatform.Api.IntegrationTests.Infrastructure;

public sealed class FakeCaptchaValidator : ICaptchaValidator
{
    public const string ValidToken = "valid-captcha-token";

    public Task<bool> IsValidAsync(
        string token,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(token == ValidToken);
    }
}