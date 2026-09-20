using System.Net.Mail;

namespace CommentsPlatform.Domain;

public sealed class Comment
{
    public Guid Id { get; }

    public string UserName { get; }

    public string Email { get; }

    public string Message { get; }

    public string? HomePage { get; }

    public DateTimeOffset CreatedAt { get; }

    public Guid? ParentCommentId { get; }

    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();

    private readonly List<Attachment> _attachments = new();

    private Comment(
        Guid id,
        string userName,
        string email,
        string? homePage,
        string message,
        DateTimeOffset createdAt,
        Guid? parentCommentId)
    {
        Id = id;
        UserName = userName;
        Email = email;
        HomePage = homePage;
        Message = message;
        CreatedAt = createdAt;
        ParentCommentId = parentCommentId;
    }

    public static Comment Create(
        string userName,
        string email,
        string? homePage,
        string message,
        Guid? parentCommentId)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException(
                "User name is required.",
                nameof(userName));
        }

        if (!userName.All(IsLatinLetterOrDigit))
        {
            throw new ArgumentException(
                "User name can contain only Latin letters and digits.",
                nameof(userName)
            );
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Email is required.",
                nameof(email));
        }

        var normalizedEmail = email.Trim();

        if (!MailAddress.TryCreate(normalizedEmail, out var parsedEmail) ||
            parsedEmail.Address != normalizedEmail)
        {
            throw new ArgumentException(
                "Email format is invalid.",
                nameof(email));
        }

        string? normalizedHomePage = null;

        if (!string.IsNullOrWhiteSpace(homePage))
        {
            if (!Uri.TryCreate(homePage, UriKind.Absolute, out var homePageUri) ||
                (homePageUri.Scheme != Uri.UriSchemeHttp &&
                 homePageUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException(
                    "Home page format is invalid.",
                    nameof(homePage));
            }

            normalizedHomePage = homePageUri.AbsoluteUri;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Message is required.",
                nameof(message)
            );
        }

        if (parentCommentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parent comment id cannot be empty.",
                nameof(parentCommentId));
        }

        var id = Guid.NewGuid();

        if (parentCommentId == id)
        {
            throw new ArgumentException(
                "A comment cannot reference itself as its parent.",
                nameof(parentCommentId));
        }

        return new Comment(
            id,
            userName,
            normalizedEmail,
            normalizedHomePage,
            message,
            DateTimeOffset.UtcNow,
            parentCommentId);
    }

    public Attachment AddAttachment(
        string originalFileName,
        string storageKey,
        string contentType,
        long fileSizeBytes,
        int? width,
        int? height)
    {
        var attachment = Attachment.Create(
            Id,
            originalFileName,
            storageKey,
            contentType,
            fileSizeBytes,
            width,
            height);

        _attachments.Add(attachment);
        return attachment;
    }

    private static bool IsLatinLetterOrDigit(char value)
    {
        return (value >= 'A' && value <= 'Z') ||
               (value >= 'a' && value <= 'z') ||
               (value >= '0' && value <= '9');
    }
}
