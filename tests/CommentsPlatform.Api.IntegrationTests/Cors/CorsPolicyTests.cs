using System.Net;
using CommentsPlatform.Api.IntegrationTests.Infrastructure;

namespace CommentsPlatform.Api.IntegrationTests.Cors;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class CorsPolicyTests
{
    private const string CorsAllowOriginHeader =
        "Access-Control-Allow-Origin";

    private readonly HttpClient _client;

    public CorsPolicyTests(
        CommentsPlatformWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Request_FromAllowedAngularOrigin_ReturnsCorsHeader()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/comments");

        request.Headers.Add(
            "Origin",
            CommentsPlatformWebApplicationFactory.AllowedCorsOrigin);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues(
                CorsAllowOriginHeader,
                out var values));

        Assert.Equal(
            CommentsPlatformWebApplicationFactory.AllowedCorsOrigin,
            Assert.Single(values));
    }

    [Fact]
    public async Task Preflight_FromAllowedAngularOrigin_ReturnsCorsHeaders()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/comments");

        request.Headers.Add(
            "Origin",
            CommentsPlatformWebApplicationFactory.AllowedCorsOrigin);
        request.Headers.Add(
            "Access-Control-Request-Method",
            "POST");
        request.Headers.Add(
            "Access-Control-Request-Headers",
            "content-type");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Assert.Equal(
            CommentsPlatformWebApplicationFactory.AllowedCorsOrigin,
            Assert.Single(
                response.Headers.GetValues(CorsAllowOriginHeader)));

        Assert.Contains(
            response.Headers.GetValues(
                "Access-Control-Allow-Methods"),
            value => value.Contains(
                "POST",
                StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            response.Headers.GetValues(
                "Access-Control-Allow-Headers"),
            value => value.Contains(
                "content-type",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Request_FromDisallowedOrigin_DoesNotReturnCorsHeader()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/comments");

        request.Headers.Add(
            "Origin",
            "https://untrusted.example");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(CorsAllowOriginHeader));
    }

    [Fact]
    public async Task Request_WithoutOrigin_DoesNotReturnCorsHeader()
    {
        using var response = await _client.GetAsync("/api/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(CorsAllowOriginHeader));
    }
}
