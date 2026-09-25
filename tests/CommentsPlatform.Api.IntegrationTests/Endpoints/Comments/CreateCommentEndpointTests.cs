using System.Net;
using System.Net.Http.Json;
using CommentsPlatform.Api.Contracts.Comments;
using CommentsPlatform.Api.IntegrationTests.Infrastructure;
using CommentsPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsPlatform.Api.IntegrationTests.Endpoints.Comments;

public sealed class CreateCommentEndpointTests
    : IClassFixture<CommentsPlatformWebApplicationFactory>
{
    private readonly CommentsPlatformWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CreateCommentEndpointTests(
        CommentsPlatformWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateComment_WithValidCaptcha_CreatesComment()
    {
        await ClearCommentsAsync();

        var request = new CreateCommentRequest(
            "User1",
            "user1@example.com",
            null,
            "Integration test comment",
            null,
            FakeCaptchaValidator.ValidToken);

        var response = await _client.PostAsJsonAsync(
            "/api/comments",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<CreateCommentResponse>();

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);

        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var storedComment = await dbContext.Comments
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(result.Id, storedComment.Id);
        Assert.Equal(request.UserName, storedComment.UserName);
        Assert.Equal(request.Email, storedComment.Email);
        Assert.Equal(request.Message, storedComment.Message);
    }

    [Fact]
    public async Task CreateComment_WithInvalidCaptcha_ReturnsBadRequestAndDoesNotPersistComment()
    {
        await ClearCommentsAsync();

        var request = new CreateCommentRequest(
            "User1",
            "user1@example.com",
            null,
            "Integration test comment",
            null,
            "invalid-captcha-token");

        var response = await _client.PostAsJsonAsync(
            "/api/comments",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var commentsExist = await dbContext.Comments
            .AsNoTracking()
            .AnyAsync();

        Assert.False(commentsExist);
    }

    private async Task ClearCommentsAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await dbContext.Comments.ExecuteDeleteAsync();
    }
}