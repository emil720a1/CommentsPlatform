namespace CommentsPlatform.Infrastructure.Caching;

public sealed class CommentsCacheOptions
{
    public const string SectionName = "CommentsCache";

    public bool Enabled { get; init; } = true;

    public int DurationSeconds { get; init; } = 60;
}
