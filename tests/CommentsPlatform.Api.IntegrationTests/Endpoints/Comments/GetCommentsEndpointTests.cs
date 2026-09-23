using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommentsPlatform.Api.Contracts.Comments.GetComments;
using CommentsPlatform.Api.IntegrationTests.Infrastructure;
using CommentsPlatform.Domain;
using CommentsPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsPlatform.Api.IntegrationTests.Endpoints.Comments;

public sealed class GetCommentsEndpointTests
    : IClassFixture<CommentsPlatformWebApplicationFactory>
{
    private readonly CommentsPlatformWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetCommentsEndpointTests(
        CommentsPlatformWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetComments_WithDefaultParameters_ReturnsOkWithEmptyPage()
    {
        await ClearCommentsAsync();

        var response = await _client.GetAsync("/api/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<GetCommentsResponse>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Theory]
    [InlineData("/api/comments?page=0")]
    [InlineData("/api/comments?pageSize=0")]
    [InlineData("/api/comments?pageSize=101")]
    [InlineData("/api/comments?sortBy=999")]
    [InlineData("/api/comments?sortDirection=999")]
    public async Task GetComments_WithInvalidQueryParameters_ReturnsBadRequest(
        string invalidQuery)
    {
        var response = await _client.GetAsync(invalidQuery);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetComments_WithStoredComments_ReturnsRequestedPageInExpectedOrder()
    {
        await ClearCommentsAsync();

        var oldestComment = Comment.Create(
            userName: "User1",
            email: "user1@example.com",
            homePage: null,
            message: "Oldest comment",
            parentCommentId: null);

        await Task.Delay(10);

        var middleComment = Comment.Create(
            userName: "User2",
            email: "user2@example.com",
            homePage: "https://example.com",
            message: "Middle comment",
            parentCommentId: null);

        await Task.Delay(10);

        var newestComment = Comment.Create(
            userName: "User3",
            email: "user3@example.com",
            homePage: null,
            message: "Newest comment",
            parentCommentId: null);

        await SeedCommentsAsync(
            oldestComment,
            middleComment,
            newestComment);

        var response = await _client.GetAsync(
            "/api/comments?page=1&pageSize=2&sortBy=CreatedAt&sortDirection=Descending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            "\"email\"",
            json,
            StringComparison.OrdinalIgnoreCase);

        var result = JsonSerializer.Deserialize<GetCommentsResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.NotNull(result);

        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);

        Assert.Equal(2, result.Items.Count);

        Assert.Equal(newestComment.Id, result.Items[0].Id);
        Assert.Equal(newestComment.UserName, result.Items[0].UserName);
        Assert.Equal(newestComment.Message, result.Items[0].Message);

        Assert.Equal(middleComment.Id, result.Items[1].Id);
        Assert.Equal(middleComment.UserName, result.Items[1].UserName);
        Assert.Equal(middleComment.Message, result.Items[1].Message);

        Assert.DoesNotContain(
            result.Items,
            comment => comment.Id == oldestComment.Id);
    }

    private async Task SeedCommentsAsync(params Comment[] comments)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        dbContext.Comments.AddRange(comments);

        await dbContext.SaveChangesAsync();
    }

    private async Task ClearCommentsAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await dbContext.Comments.ExecuteDeleteAsync();
    }
}
