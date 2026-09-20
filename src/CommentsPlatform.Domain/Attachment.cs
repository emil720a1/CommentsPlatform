namespace CommentsPlatform.Domain;

public sealed class Attachment
{
    public Guid Id { get; }
    public Guid CommentId { get; }

    public string OriginalFileName { get; }
    public string StorageKey { get; }
    public string ContentType { get; }

    public long FileSizeBytes { get; }

    public int? Width { get; }
    public int? Height { get; }

    public DateTimeOffset CreatedAt { get; }

    private Attachment(
        Guid id,
        Guid commentId,
        string originalFileName,
        string storageKey,
        string contentType,
        long fileSizeBytes,
        int? width,
        int? height,
        DateTimeOffset createdAt)
    {
        Id = id;
        CommentId = commentId;
        OriginalFileName = originalFileName;
        StorageKey = storageKey;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        Width = width;
        Height = height;
        CreatedAt = createdAt;
    }

    internal static Attachment Create(
        Guid commentId,
        string originalFileName,
        string storageKey,
        string contentType,
        long fileSizeBytes,
        int? width,
        int? height)
    {
        if (commentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Comment id cannot be empty.",
                nameof(commentId));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException(
                "Original file name is required.",
                nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException(
                "Storage key is required.",
                nameof(storageKey));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException(
                "Content type is required.",
                nameof(contentType));
        }

        if (fileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fileSizeBytes),
                "File size must be greater than zero.");
        }

        if ((width is null) != (height is null))
        {
            throw new ArgumentException(
                "Width and height must be specified together.");
        }

        if (width is <= 0 || height is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Image dimensions must be greater than zero.");
        }

        return new Attachment(
            Guid.NewGuid(),
            commentId,
            originalFileName,
            storageKey,
            contentType,
            fileSizeBytes,
            width,
            height,
            DateTimeOffset.UtcNow);
    }
}
