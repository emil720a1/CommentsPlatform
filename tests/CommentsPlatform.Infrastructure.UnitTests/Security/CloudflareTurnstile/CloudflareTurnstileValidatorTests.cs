using System.Net;
using System.Text;
using CommentsPlatform.Infrastructure.Security.CloudflareTurnstile;
using Microsoft.Extensions.Options;

namespace CommentsPlatform.Infrastructure.UnitTests.Security.CloudflareTurnstile;

public sealed class CloudflareTurnstileValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsValidAsync_WithMissingToken_ReturnsFalseWithoutHttpRequest(
        string token)
    {
        var handler = new CountingHttpMessageHandler();
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            token,
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task IsValidAsync_WithSuccessfulProviderResponse_ReturnsTrue()
    {
        var providerResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                    "success": true
                }
                """,
                Encoding.UTF8,
                "application/json")
        };

        var handler = new CountingHttpMessageHandler(providerResponse);
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            "valid-token",
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task IsValidAsync_WithUnsuccessfulProviderResponse_ReturnsFalse()
    {
        var providerResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "success": false,
                  "error-codes": [
                    "invalid-input-response"
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        };

        var handler = new CountingHttpMessageHandler(providerResponse);
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            "invalid-token",
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task IsValidAsync_WithUnsuccessfulHttpStatus_ReturnsFalse(
        HttpStatusCode statusCode)
    {
        var providerResponse = new HttpResponseMessage(statusCode);
        var handler = new CountingHttpMessageHandler(providerResponse);
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            "captcha-token",
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task IsValidAsync_WithMalformedJson_ReturnsFalse()
    {
        var providerResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{ invalid-json",
                Encoding.UTF8,
                "application/json")
        };

        var handler = new CountingHttpMessageHandler(providerResponse);
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            "captcha-token",
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task IsValidAsync_WithUnsupportedContentType_ReturnsFalse()
    {
        var providerResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "unexpected response",
                Encoding.UTF8,
                "text/plain")
        };

        var handler = new CountingHttpMessageHandler(providerResponse);
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            "captcha-token",
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task IsValidAsync_WithNetworkFailure_ReturnsFalse()
    {
        var handler = new CountingHttpMessageHandler(
            (_, _) => Task.FromException<HttpResponseMessage>(
                new HttpRequestException(
                    "Simulated network failure.")));

        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            "captcha-token",
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task IsValidAsync_WithProviderTimeout_ReturnsFalse()
    {
        var handler = new CountingHttpMessageHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            TimeoutSeconds = 1
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            "captcha-token",
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task IsValidAsync_WithCallerCancellation_PropagatesCancellation()
    {
        var handler = new CountingHttpMessageHandler(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var httpClient = new HttpClient(handler);
        using var cancellationTokenSource = new CancellationTokenSource();

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            validator.IsValidAsync(
                "captcha-token",
                cancellationTokenSource.Token));

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task IsValidAsync_WithUnexpectedHostname_ReturnsFalse()
    {
        var providerResponse = CreateSuccessfulResponse(
            hostname: "unexpected.example.test",
            action: "create-comment");
        var handler = new CountingHttpMessageHandler(providerResponse);
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            ExpectedHostname = "comments.example.test",
            ExpectedAction = "create-comment",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            "captcha-token",
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task IsValidAsync_WithUnexpectedAction_ReturnsFalse()
    {
        var providerResponse = CreateSuccessfulResponse(
            hostname: "comments.example.test",
            action: "login");
        var handler = new CountingHttpMessageHandler(providerResponse);
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            ExpectedHostname = "comments.example.test",
            ExpectedAction = "create-comment",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            "captcha-token",
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task IsValidAsync_WithToken_SendsExpectedProviderRequest()
    {
        HttpMethod? capturedMethod = null;
        Uri? capturedRequestUri = null;
        string? capturedBody = null;

        var handler = new CountingHttpMessageHandler(
            async (request, cancellationToken) =>
            {
                capturedMethod = request.Method;
                capturedRequestUri = request.RequestUri;
                capturedBody = await request.Content!
                    .ReadAsStringAsync(cancellationToken);

                return CreateSuccessfulResponse();
            });

        var httpClient = new HttpClient(handler);

        var options = Options.Create(new CloudflareTurnstileOptions
        {
            SecretKey = "test-secret",
            VerificationUrl = "https://example.test/siteverify",
            TimeoutSeconds = 10
        });

        var validator = new CloudflareTurnstileValidator(
            httpClient,
            options);

        var result = await validator.IsValidAsync(
            "captcha-token",
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(HttpMethod.Post, capturedMethod);
        Assert.Equal(
            new Uri("https://example.test/siteverify"),
            capturedRequestUri);
        Assert.NotNull(capturedBody);
        Assert.Contains("secret=test-secret", capturedBody);
        Assert.Contains("response=captcha-token", capturedBody);
        Assert.Equal(1, handler.CallCount);
    }

    private static HttpResponseMessage CreateSuccessfulResponse(
        string? hostname = null,
        string? action = null)
    {
        var json = $$"""
                     {
                       "success": true,
                       "hostname": {{ToJsonString(hostname)}},
                       "action": {{ToJsonString(action)}}
                     }
                     """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json")
        };
    }

    private static string ToJsonString(string? value) =>
        value is null ? "null" : $"\"{value}\"";

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>> _sendAsync;

        public CountingHttpMessageHandler(
            HttpResponseMessage? response = null)
        {
            var configuredResponse = response ??
                new HttpResponseMessage(HttpStatusCode.OK);

            _sendAsync = (_, _) =>
                Task.FromResult(configuredResponse);
        }

        public CountingHttpMessageHandler(
            Func<
                HttpRequestMessage,
                CancellationToken,
                Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;

            return _sendAsync(request, cancellationToken);
        }
    }
}
