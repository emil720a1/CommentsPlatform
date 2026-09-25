using CommentsPlatform.Infrastructure.Security.HtmlSanitization;

namespace CommentsPlatform.Infrastructure.UnitTests.HtmlSanitization;

public sealed class HtmlSanitizerServiceTests
{
    private readonly HtmlSanitizerService _sanitizer = new();

    [Fact]
    public void Sanitize_WithAllowedHtml_PreservesAllowedElements()
    {
        const string html =
            "<p>Hello <strong>world</strong></p>";

        var result = _sanitizer.Sanitize(html);

        Assert.Equal(html, result);
    }

    [Fact]
    public void Sanitize_WithScriptElement_RemovesScriptElement()
    {
        const string html =
            "<p>Safe</p><script>alert('xss')</script>";

        var result = _sanitizer.Sanitize(html);

        Assert.Equal("<p>Safe</p>", result);
        Assert.DoesNotContain(
            "script",
            result,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "alert",
            result,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_WithEventAttribute_RemovesEventAttribute()
    {
        const string html =
            """<p onclick="alert('xss')">Safe</p>""";

        var result = _sanitizer.Sanitize(html);

        Assert.Equal("<p>Safe</p>", result);
        Assert.DoesNotContain(
            "onclick",
            result,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_WithJavaScriptUrl_RemovesUnsafeUrl()
    {
        const string html =
            """<a href="javascript:alert('xss')" title="Link">Click</a>""";

        var result = _sanitizer.Sanitize(html);

        Assert.Equal("""<a title="Link">Click</a>""", result);
        Assert.DoesNotContain(
            "javascript",
            result,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_WithHttpsUrl_PreservesSafeLink()
    {
        const string html =
            """<a href="https://example.com" title="Example">Visit</a>""";

        var result = _sanitizer.Sanitize(html);

        Assert.Equal(html, result);
    }

    [Fact]
    public void Sanitize_WithDisallowedElement_RemovesElementAndItsContent()
    {
        const string html =
            "<p>Before</p><marquee>Visible text</marquee>";

        var result = _sanitizer.Sanitize(html);

        Assert.Equal("<p>Before</p>", result);
        Assert.DoesNotContain(
            "marquee",
            result,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "Visible text",
            result,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_WithImageOnError_RemovesUnsafeImage()
    {
        const string html =
            """<p>Safe</p><img src="invalid" onerror="alert('xss')">""";

        var result = _sanitizer.Sanitize(html);

        Assert.Equal("<p>Safe</p>", result);
        Assert.DoesNotContain(
            "img",
            result,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "onerror",
            result,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "alert",
            result,
            StringComparison.OrdinalIgnoreCase);
    }
}
