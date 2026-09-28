namespace CommentsPlatform.Infrastructure.Storage;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; init; } = string.Empty;
}