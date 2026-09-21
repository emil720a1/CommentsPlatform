using CommentsPlatform.Application.Features.Comments.Create;

namespace CommentsPlatform.Application.UnitTests.Features.Comments.Create;

public sealed class CreateCommentCommandValidatorTests
{
    private readonly CreateCommentCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ReturnsNoErrors()
    {
        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            Guid.NewGuid());

        var result = await _validator.ValidateAsync(command, CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Validate_WithMissingUserName_ReturnsRequiredError(
        string? invalidUserName)
    {
        var command = new CreateCommentCommand(
            invalidUserName!,
            "user@example.com",
            "https://example.com/",
            "Test message",
            Guid.NewGuid());

        var result = await _validator.ValidateAsync(command, CancellationToken.None);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Comments.UserName.Required", error.ErrorCode);
    }

    [Theory]
    [InlineData("user_name")]
    [InlineData("user-name")]
    [InlineData("Користувач")]
    [InlineData("user!")]
    public async Task Validate_WithInvalidUserNameCharacters_ReturnsInvalidCharactersError(
        string invalidUserName)
    {
        var command = new CreateCommentCommand(
            invalidUserName,
            "user@example.com",
            "https://example.com/",
            "Test message",
            Guid.NewGuid());

        var result = await _validator.ValidateAsync(command, CancellationToken.None);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Comments.UserName.InvalidCharacters", error.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Validate_WithMissingEmail_ReturnsRequiredError(
        string? invalidEmail)
    {
        var command = new CreateCommentCommand(
            "user123",
            invalidEmail!,
            "https://example.com/",
            "Test message",
            Guid.NewGuid());

        var result = await _validator.ValidateAsync(command, CancellationToken.None);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Comments.Email.Required", error.ErrorCode);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    public async Task Validate_WithInvalidEmail_ReturnsInvalidFormatError(
        string invalidEmail)
    {
        var command = new CreateCommentCommand(
            "user123",
            invalidEmail,
            "https://example.com/",
            "Test message",
            Guid.NewGuid());

        var result = await _validator.ValidateAsync(command, CancellationToken.None);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Comments.Email.InvalidFormat", error.ErrorCode);
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("not-a-url")]
    public async Task Validate_WithInvalidHomePage_ReturnsInvalidFormatError(
        string invalidHomePage)
    {
        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            invalidHomePage,
            "Test message",
            Guid.NewGuid());

        var result = await _validator.ValidateAsync(command, CancellationToken.None);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Comments.HomePage.InvalidFormat", error.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Validate_WithMissingHomePage_ReturnsNoErrors(
        string? homePage)
    {
        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            homePage,
            "Test message",
            Guid.NewGuid());

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Validate_WithMissingMessage_ReturnsRequiredError(
        string? invalidMessage)
    {
        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            invalidMessage!,
            Guid.NewGuid());

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Comments.Message.Required", error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WithEmptyParentCommentId_ReturnsEmptyError()
    {
        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            Guid.Empty);

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Comments.ParentCommentId.Empty",
            error.ErrorCode);
    }

    [Fact]
    public async Task Validate_WithHttpHomePage_ReturnsNoErrors()
    {
        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "http://example.com/",
            "Test message",
            Guid.NewGuid());

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Validate_WithoutParentCommentId_ReturnsNoErrors()
    {
        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            null);

        var result = await _validator.ValidateAsync(
            command,
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
