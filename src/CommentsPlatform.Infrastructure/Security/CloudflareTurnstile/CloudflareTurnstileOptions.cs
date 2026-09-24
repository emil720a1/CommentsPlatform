namespace CommentsPlatform.Infrastructure.Security.CloudflareTurnstile;

public sealed class CloudflareTurnstileOptions
{
    public const string SectionName = "CloudflareTurnstile";

    public string SecretKey { get; set; } = string.Empty;

    public string VerificationUrl { get; set; } = string.Empty;

    public string? ExpectedHostname { get; set; }

    public string? ExpectedAction { get; set; }

    public int TimeoutSeconds { get; set; } = 10;
}