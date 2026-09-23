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
    public async Task GetComments_WithDescendingSort_ReturnsNewestCommentsFirstAndOmitsEmail()
    {
        await ClearCommentsAsync();

        var oldestCreatedAt = new DateTimeOffset(
            2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var middleCreatedAt = oldestCreatedAt.AddMinutes(1);
        var newestCreatedAt = oldestCreatedAt.AddMinutes(2);

        var oldestComment = Comment.Create(
            userName: "User1",
            email: "user1@example.com",
            homePage: null,
            message: "Oldest comment",
            parentCommentId: null);

        var middleComment = Comment.Create(
            userName: "User2",
            email: "user2@example.com",
            homePage: "https://example.com",
            message: "Middle comment",
            parentCommentId: null);

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

        await SetCreatedAtAsync(
            (oldestComment, oldestCreatedAt),
            (middleComment, middleCreatedAt),
            (newestComment, newestCreatedAt));

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

    [Fact]
    public async Task GetComments_WithReplies_ReturnsOnlyTopLevelComments()
    {
        await ClearCommentsAsync();

        var parentComment = Comment.Create(
            userName: "Parent1",
            email: "parent@example.com",
            homePage: null,
            message: "Parent comment",
            parentCommentId: null);
        var secondTopLevelComment = Comment.Create(
            userName: "Parent2",
            email: "second@example.com",
            homePage: null,
            message: "Second top-level comment",
            parentCommentId: null);
        var reply = Comment.Create(
            userName: "Reply1",
            email: "reply@example.com",
            homePage: null,
            message: "Reply",
            parentCommentId: parentComment.Id);

        await SeedCommentsAsync(
            parentComment,
            secondTopLevelComment,
            reply);

        var response = await _client.GetAsync(
            "/api/comments?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<GetCommentsResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(
            result.Items,
            comment => comment.Id == parentComment.Id);
        Assert.Contains(
            result.Items,
            comment => comment.Id == secondTopLevelComment.Id);
        Assert.DoesNotContain(
            result.Items,
            comment => comment.Id == reply.Id);
    }

    [Fact]
    public async Task GetComments_WithSecondPage_ReturnsCorrectItemsAndNavigationMetadata()
    {
        await ClearCommentsAsync();

        var baseCreatedAt = new DateTimeOffset(
            2026, 2, 1, 10, 0, 0, TimeSpan.Zero);
        var comments = Enumerable.Range(1, 5)
            .Select(index => Comment.Create(
                userName: $"User{index}",
                email: $"user{index}@example.com",
                homePage: null,
                message: $"Comment {index}",
                parentCommentId: null))
            .ToArray();

        await SeedCommentsAsync(comments);
        await SetCreatedAtAsync(comments
            .Select((comment, index) =>
                (comment, baseCreatedAt.AddMinutes(index)))
            .ToArray());

        var response = await _client.GetAsync(
            "/api/comments?page=2&pageSize=2&sortBy=CreatedAt&sortDirection=Descending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<GetCommentsResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Collection(
            result.Items,
            comment => Assert.Equal(comments[2].Id, comment.Id),
            comment => Assert.Equal(comments[1].Id, comment.Id));
    }

    [Fact]
    public async Task GetComments_WithAscendingSort_ReturnsOldestCommentsFirst()
    {
        await ClearCommentsAsync();

        var oldestCreatedAt = new DateTimeOffset(
            2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var oldestComment = Comment.Create(
            userName: "Oldest1",
            email: "oldest@example.com",
            homePage: null,
            message: "Oldest comment",
            parentCommentId: null);
        var middleComment = Comment.Create(
            userName: "Middle1",
            email: "middle@example.com",
            homePage: null,
            message: "Middle comment",
            parentCommentId: null);
        var newestComment = Comment.Create(
            userName: "Newest1",
            email: "newest@example.com",
            homePage: null,
            message: "Newest comment",
            parentCommentId: null);

        await SeedCommentsAsync(
            oldestComment,
            middleComment,
            newestComment);
        await SetCreatedAtAsync(
            (oldestComment, oldestCreatedAt),
            (middleComment, oldestCreatedAt.AddMinutes(1)),
            (newestComment, oldestCreatedAt.AddMinutes(2)));

        var response = await _client.GetAsync(
            "/api/comments?page=1&pageSize=3&sortBy=CreatedAt&sortDirection=Ascending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<GetCommentsResponse>();

        Assert.NotNull(result);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
        Assert.Collection(
            result.Items,
            comment => Assert.Equal(oldestComment.Id, comment.Id),
            comment => Assert.Equal(middleComment.Id, comment.Id),
            comment => Assert.Equal(newestComment.Id, comment.Id));
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

        await dbContext.Comments
            .Where(comment => comment.ParentCommentId != null)
            .ExecuteDeleteAsync();
        await dbContext.Comments.ExecuteDeleteAsync();
    }

    private async Task SetCreatedAtAsync(
        params (Comment Comment, DateTimeOffset CreatedAt)[] comments)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        foreach (var (comment, createdAt) in comments)
        {
            await dbContext.Comments
                .Where(storedComment => storedComment.Id == comment.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        storedComment => storedComment.CreatedAt,
                        createdAt));
        }
    }
}
