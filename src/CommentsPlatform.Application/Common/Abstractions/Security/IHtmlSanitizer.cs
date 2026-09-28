namespace CommentsPlatform.Application.Common.Abstractions.Security;

public interface IHtmlSanitizer
{
    string Sanitize(string html);
}
