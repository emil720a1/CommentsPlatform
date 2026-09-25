using ApplicationHtmlSanitizer =
    CommentsPlatform.Application.Common.Abstractions.Security.IHtmlSanitizer;
using GanssHtmlSanitizer = Ganss.Xss.HtmlSanitizer;

namespace CommentsPlatform.Infrastructure.Security.HtmlSanitization;

public sealed class HtmlSanitizerService : ApplicationHtmlSanitizer
{
    private readonly GanssHtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new GanssHtmlSanitizer();

        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.UnionWith(
        [
            "p",
            "strong",
            "em",
            "ul",
            "ol",
            "li",
            "blockquote",
            "code",
            "pre",
            "a"
        ]);

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.UnionWith(
        [
            "href",
            "title"
        ]);

        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.UnionWith(
        [
            Uri.UriSchemeHttp,
            Uri.UriSchemeHttps
        ]);
    }

    public string Sanitize(string html)
    {
        return _sanitizer.Sanitize(html);
    }
}
