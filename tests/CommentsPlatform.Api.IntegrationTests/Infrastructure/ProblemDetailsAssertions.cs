using System.Net;
using System.Text.Json;

namespace CommentsPlatform.Api.IntegrationTests.Infrastructure;

internal static class ProblemDetailsAssertions
{
    public static async Task AssertAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode,
        string expectedTitle,
        string expectedDetail,
        string expectedErrorCode,
        string? expectedErrorDescription = null)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);

        await using var responseStream =
            await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(responseStream);

        var root = document.RootElement;

        Assert.Equal(
            (int)expectedStatusCode,
            root.GetProperty("status").GetInt32());
        Assert.Equal(
            expectedTitle,
            root.GetProperty("title").GetString());
        Assert.Equal(
            expectedDetail,
            root.GetProperty("detail").GetString());

        var errors = root.GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);

        var error = Assert.Single(errors.EnumerateArray());

        Assert.Equal(
            expectedErrorCode,
            error.GetProperty("code").GetString());

        var description = error
            .GetProperty("description")
            .GetString();

        if (expectedErrorDescription is null)
        {
            Assert.False(string.IsNullOrWhiteSpace(description));
        }
        else
        {
            Assert.Equal(expectedErrorDescription, description);
        }
    }
}
