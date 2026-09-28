namespace CommentsPlatform.Api.Common.Cors;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
