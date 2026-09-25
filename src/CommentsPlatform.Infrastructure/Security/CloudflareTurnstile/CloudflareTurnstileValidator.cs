using System.Net.Http.Json;
using System.Text.Json;
using CommentsPlatform.Application.Common.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace CommentsPlatform.Infrastructure.Security.CloudflareTurnstile;

public sealed class CloudflareTurnstileValidator : ICaptchaValidator
{
    private readonly HttpClient _client;
    private readonly CloudflareTurnstileOptions _options;

    public CloudflareTurnstileValidator(
        HttpClient client,
        IOptions<CloudflareTurnstileOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<bool> IsValidAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            using var timeoutCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeoutCancellationTokenSource.CancelAfter(
                TimeSpan.FromSeconds(_options.TimeoutSeconds));

            var formData = new Dictionary<string, string>
            {
                ["secret"] = _options.SecretKey,
                ["response"] = token
            };

            using var content = new FormUrlEncodedContent(formData);

            using var response = await _client.PostAsync(
                _options.VerificationUrl,
                content,
                timeoutCancellationTokenSource.Token);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content
                .ReadFromJsonAsync<CloudflareTurnstileResponse>(
                    timeoutCancellationTokenSource.Token);

            if (result is null || !result.Success)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_options.ExpectedHostname) &&
                !string.Equals(
                    result.Hostname,
                    _options.ExpectedHostname,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_options.ExpectedAction) &&
                !string.Equals(
                    result.Action,
                    _options.ExpectedAction,
                    StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}
