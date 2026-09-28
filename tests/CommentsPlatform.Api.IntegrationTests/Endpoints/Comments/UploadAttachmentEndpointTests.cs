using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommentsPlatform.Api.Contracts.Comments.Attachments;
using CommentsPlatform.Api.IntegrationTests.Infrastructure;
using CommentsPlatform.Domain;
using CommentsPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsPlatform.Api.IntegrationTests.Endpoints.Comments;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class UploadAttachmentEndpointTests
{
    private static readonly DateTimeOffset FixedCreatedAt =
        new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly CommentsPlatformWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UploadAttachmentEndpointTests(
        CommentsPlatformWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UploadAttachment_WithValidFile_PersistsMetadataAndFile()
    {
        await ResetStateAsync();
        var comment = await SeedCommentAsync();
        byte[] expectedContent = [1, 2, 3, 4];
        using var requestContent = CreateMultipartContent(
            expectedContent,
            "image.png",
            "image/png");

        using var response = await _client.PostAsync(
            $"/api/comments/{comment.Id}/attachments",
            requestContent);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<UploadAttachmentResponse>();

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.AttachmentId);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var attachment = await dbContext.Attachments
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(result.AttachmentId, attachment.Id);
        Assert.Equal(comment.Id, attachment.CommentId);
        Assert.Equal("image.png", attachment.OriginalFileName);
        Assert.Equal("image/png", attachment.ContentType);
        Assert.Equal(expectedContent.LongLength, attachment.FileSizeBytes);
        Assert.Null(attachment.Width);
        Assert.Null(attachment.Height);
        Assert.StartsWith(
            $"comments/{comment.Id:N}/",
            attachment.StorageKey,
            StringComparison.Ordinal);
        Assert.EndsWith(
            ".png",
            attachment.StorageKey,
            StringComparison.Ordinal);

        var filePath = GetStoredFilePath(attachment.StorageKey);

        Assert.True(File.Exists(filePath));
        Assert.Equal(
            expectedContent,
            await File.ReadAllBytesAsync(filePath));
    }

    [Fact]
    public async Task UploadAttachment_WithMissingComment_ReturnsNotFoundAndStoresNothing()
    {
        await ResetStateAsync();
        using var requestContent = CreateMultipartContent(
            [1],
            "image.png",
            "image/png");

        using var response = await _client.PostAsync(
            $"/api/comments/{Guid.NewGuid()}/attachments",
            requestContent);

        await ProblemDetailsAssertions.AssertAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            "The comment was not found.",
            "Attachments.CommentNotFound",
            "The comment was not found.");

        Assert.False(await AttachmentsExistAsync());
        AssertStorageIsEmpty();
    }

    [Fact]
    public async Task UploadAttachment_WithUnsupportedContentType_ReturnsBadRequestAndStoresNothing()
    {
        await ResetStateAsync();
        var comment = await SeedCommentAsync();
        using var requestContent = CreateMultipartContent(
            [1],
            "malware.exe",
            "application/executable");

        using var response = await _client.PostAsync(
            $"/api/comments/{comment.Id}/attachments",
            requestContent);

        await ProblemDetailsAssertions.AssertAsync(
            response,
            HttpStatusCode.BadRequest,
            "Validation error",
            "One or more validation errors occurred.",
            "Attachments.ContentType.Unsupported",
            "The attachment content type is not supported.");

        Assert.False(await AttachmentsExistAsync());
        AssertStorageIsEmpty();
    }

    [Fact]
    public async Task UploadAttachment_WithEmptyFile_ReturnsBadRequestAndStoresNothing()
    {
        await ResetStateAsync();
        var comment = await SeedCommentAsync();
        using var requestContent = CreateMultipartContent(
            [],
            "empty.txt",
            "text/plain");

        using var response = await _client.PostAsync(
            $"/api/comments/{comment.Id}/attachments",
            requestContent);

        await ProblemDetailsAssertions.AssertAsync(
            response,
            HttpStatusCode.BadRequest,
            "Validation error",
            "One or more validation errors occurred.",
            "Attachments.FileSize.Empty");

        Assert.False(await AttachmentsExistAsync());
        AssertStorageIsEmpty();
    }

    [Fact]
    public async Task UploadAttachment_WithoutFile_ReturnsBadRequestAndStoresNothing()
    {
        await ResetStateAsync();
        var comment = await SeedCommentAsync();
        using var requestContent = new MultipartFormDataContent();

        using var response = await _client.PostAsync(
            $"/api/comments/{comment.Id}/attachments",
            requestContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await AttachmentsExistAsync());
        AssertStorageIsEmpty();
    }

    [Theory]
    [InlineData("../image.png")]
    [InlineData(@"..\image.png")]
    public async Task UploadAttachment_WithUnsafeFileName_ReturnsBadRequestAndStoresNothing(
        string fileName)
    {
        await ResetStateAsync();
        var comment = await SeedCommentAsync();
        using var requestContent = CreateMultipartContent(
            [1],
            fileName,
            "image/png");

        using var response = await _client.PostAsync(
            $"/api/comments/{comment.Id}/attachments",
            requestContent);

        await ProblemDetailsAssertions.AssertAsync(
            response,
            HttpStatusCode.BadRequest,
            "Validation error",
            "One or more validation errors occurred.",
            "Attachments.FileName.Invalid");

        Assert.False(await AttachmentsExistAsync());
        AssertStorageIsEmpty();
    }

    private async Task<Comment> SeedCommentAsync()
    {
        var comment = Comment.Create(
            "user123",
            "user@example.com",
            null,
            "Integration test comment",
            null,
            FixedCreatedAt);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync();

        return comment;
    }

    private async Task<bool> AttachmentsExistAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        return await dbContext.Attachments
            .AsNoTracking()
            .AnyAsync();
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
            Directory.Delete(
                _factory.FileStorageRootPath,
                recursive: true);
        }
    }

    private void AssertStorageIsEmpty()
    {
        if (!Directory.Exists(_factory.FileStorageRootPath))
        {
            return;
        }

        Assert.Empty(Directory.EnumerateFiles(
            _factory.FileStorageRootPath,
            "*",
            SearchOption.AllDirectories));
    }

    private string GetStoredFilePath(string storageKey)
    {
        return Path.Combine(
            _factory.FileStorageRootPath,
            Path.Combine(storageKey.Split('/')));
    }

    private static MultipartFormDataContent CreateMultipartContent(
        byte[] content,
        string fileName,
        string contentType)
    {
        var multipartContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipartContent.Add(fileContent, "File", fileName);

        return multipartContent;
    }
}
