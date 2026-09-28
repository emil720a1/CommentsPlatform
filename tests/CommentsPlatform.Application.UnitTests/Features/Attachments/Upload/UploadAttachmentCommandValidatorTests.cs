using CommentsPlatform.Application.Features.Attachments.Upload;

namespace CommentsPlatform.Application.UnitTests.Features.Attachments.Upload;

public sealed class UploadAttachmentCommandValidatorTests
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly UploadAttachmentCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ReturnsNoErrors()
    {
        await using var content = new MemoryStream([1, 2, 3]);

        var command = CreateValidCommand(content);

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WithEmptyCommentId_ReturnsRequiredError()
    {
        await using var content = new MemoryStream([1]);

        var command = CreateValidCommand(content) with
        {
            CommentId = Guid.Empty
        };

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "Attachments.CommentId.Required",
            error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WithMissingContent_ReturnsRequiredError()
    {
        var command = CreateValidCommand(Stream.Null) with
        {
            Content = null!
        };

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "Attachments.Content.Required",
            error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WithUnreadableContent_ReturnsNotReadableError()
    {
        var content = new MemoryStream([1]);
        await content.DisposeAsync();

        var command = CreateValidCommand(content);

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "Attachments.Content.NotReadable",
            error.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Validate_WithMissingFileName_ReturnsRequiredError(
        string? fileName)
    {
        await using var content = new MemoryStream([1]);

        var command = CreateValidCommand(content) with
        {
            OriginalFileName = fileName!
        };

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "Attachments.FileName.Required",
            error.ErrorCode);
    }
    [Theory]
    [InlineData("../image.png")]
    [InlineData(@"..\image.png")]
    [InlineData("folder/image.png")]
    [InlineData(@"folder\image.png")]
    public async Task Validate_WithUnsafeFileName_ReturnsInvalidError(
        string fileName)
    {
        await using var content = new MemoryStream([1]);

        var command = CreateValidCommand(content) with
        {
            OriginalFileName = fileName
        };

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "Attachments.FileName.Invalid",
            error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WithTooLongFileName_ReturnsTooLongError()
    {
        await using var content = new MemoryStream([1]);

        var command = CreateValidCommand(content) with
        {
            OriginalFileName = $"{new string('a', 252)}.png"
        };

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "Attachments.FileName.TooLong",
            error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WithUnsupportedContentType_ReturnsUnsupportedError()
    {
        await using var content = new MemoryStream([1]);

        var command = CreateValidCommand(content) with
        {
            ContentType = "application/executable"
        };

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "Attachments.ContentType.Unsupported",
            error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WithDifferentContentTypeCasing_ReturnsNoErrors()
    {
        await using var content = new MemoryStream([1]);

        var command = CreateValidCommand(content) with
        {
            ContentType = "IMAGE/PNG"
        };

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WithEmptyFile_ReturnsEmptyError()
    {
        await using var content = new MemoryStream();

        var command = CreateValidCommand(content) with
        {
            FileSizeBytes = 0
        };

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "Attachments.FileSize.Empty",
            error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WithFileLargerThanLimit_ReturnsTooLargeError()
    {
        await using var content = new MemoryStream([1]);

        var command = CreateValidCommand(content) with
        {
            FileSizeBytes = MaxFileSizeBytes + 1
        };

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal(
            "Attachments.FileSize.TooLarge",
            error.ErrorCode);
    }

    private static UploadAttachmentCommand CreateValidCommand(
        Stream content)
    {
        return new UploadAttachmentCommand(
            Guid.NewGuid(),
            content,
            "image.png",
            "image/png",
            3);
    }
}