using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommentsPlatform.Api.Contracts.Comments.Attachments;
using CommentsPlatform.Api.Contracts.Comments.GetComments;
using CommentsPlatform.Api.IntegrationTests.Infrastructure;
using CommentsPlatform.Domain;
using CommentsPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsPlatform.Api.IntegrationTests.Endpoints.Comments;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class DownloadAttachmentEndpointTests
{
    private readonly CommentsPlatformWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DownloadAttachmentEndpointTests(
        CommentsPlatformWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetComments_WithAttachment_ReturnsSafeMetadataAndDownloadUrl()
    {
        var (comment, attachmentId, _) = await SeedUploadedAttachmentAsync();

        var response = await _client.GetFromJsonAsync<GetCommentsResponse>(
            "/api/comments");

        Assert.NotNull(response);
        var returnedComment = Assert.Single(response.Items);
        var attachment = Assert.Single(returnedComment.Attachments);
        Assert.Equal(attachmentId, attachment.Id);
        Assert.Equal("notes.txt", attachment.OriginalFileName);
        Assert.Equal("text/plain", attachment.ContentType);
        Assert.Equal(4, attachment.FileSizeBytes);
        Assert.Equal(
            $"/api/comments/{comment.Id}/attachments/{attachmentId}",
            attachment.DownloadUrl);

        var json = await _client.GetStringAsync("/api/comments");
        Assert.DoesNotContain("storageKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(_factory.FileStorageRootPath, json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DownloadAttachment_WhenItExists_ReturnsStoredFileAndHeaders()
    {
        var (comment, attachmentId, expectedContent) =
            await SeedUploadedAttachmentAsync();

        using var response = await _client.GetAsync(
            $"/api/comments/{comment.Id}/attachments/{attachmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            "notes.txt",
            response.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.Equal(expectedContent, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task DownloadAttachment_WhenAttachmentIsMissing_ReturnsNotFound()
    {
        await ResetStateAsync();

        using var response = await _client.GetAsync(
            $"/api/comments/{Guid.NewGuid()}/attachments/{Guid.NewGuid()}");

        await ProblemDetailsAssertions.AssertAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            "The attachment was not found.",
            "Attachments.NotFound",
            "The attachment was not found.");
    }

    [Fact]
    public async Task DownloadAttachment_WithDifferentCommentId_ReturnsNotFound()
    {
        var (_, attachmentId, _) = await SeedUploadedAttachmentAsync();

        using var response = await _client.GetAsync(
            $"/api/comments/{Guid.NewGuid()}/attachments/{attachmentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(Comment Comment, Guid AttachmentId, byte[] Content)>
        SeedUploadedAttachmentAsync()
    {
        await ResetStateAsync();
        var comment = Comment.Create(
            "user123",
            "user@example.com",
            null,
            "Attachment integration test",
            null,
            new DateTimeOffset(2026, 9, 27, 10, 0, 0, TimeSpan.Zero));

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            dbContext.Comments.Add(comment);
            await dbContext.SaveChangesAsync();
        }

        byte[] content = [1, 2, 3, 4];
        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(fileContent, "File", "notes.txt");

        using var uploadResponse = await _client.PostAsync(
            $"/api/comments/{comment.Id}/attachments",
            multipart);
        uploadResponse.EnsureSuccessStatusCode();
        var upload = await uploadResponse.Content
            .ReadFromJsonAsync<UploadAttachmentResponse>();

        return (comment, Assert.IsType<UploadAttachmentResponse>(upload).AttachmentId, content);
    }

    private async Task ResetStateAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await dbContext.Attachments.ExecuteDeleteAsync();
        await dbContext.Comments.ExecuteDeleteAsync();

        if (Directory.Exists(_factory.FileStorageRootPath))
        {
            Directory.Delete(_factory.FileStorageRootPath, recursive: true);
        }
    }
}
