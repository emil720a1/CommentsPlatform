using System.Text.Json.Serialization;

namespace CommentsPlatform.Infrastructure.Security.CloudflareTurnstile;

internal sealed class CloudflareTurnstileResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("error-codes")]
    public string[] ErrorCodes { get; set; } = [];
}