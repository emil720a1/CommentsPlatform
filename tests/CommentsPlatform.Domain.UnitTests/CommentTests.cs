namespace CommentsPlatform.Domain.UnitTests;

public sealed class CommentTests
{
    [Fact]
    public void Create_WithValidTopLevelComment_ReturnsExpectedComment()
    {
        const string username = "username";
        const string email = "userEmail@gmail.com";
        const string homePage = "https://example.com/";
        const string message = "message";

        var beforeCreation = DateTimeOffset.UtcNow;

        var comment = Comment.Create(
            username,
            email,
            homePage,
            message,
            null);

        var afterCreation = DateTimeOffset.UtcNow;

        Assert.NotEqual(Guid.Empty, comment.Id);
        Assert.Equal(username, comment.UserName);
        Assert.Equal(email, comment.Email);
        Assert.Equal(homePage, comment.HomePage);
        Assert.Equal(message, comment.Message);
        Assert.Null(comment.ParentCommentId);
        Assert.Equal(TimeSpan.Zero, comment.CreatedAt.Offset);
        Assert.InRange(comment.CreatedAt, beforeCreation, afterCreation);
    }

    [Fact]
    public void Create_WithValidParentCommentId_ReturnsReply()
    {
        const string username = "username";
        const string email = "userEmail@gmail.com";
        const string message = "message";
        var parentCommentId = Guid.NewGuid();

        var comment = Comment.Create(
            username,
            email,
            null,
            message,
            parentCommentId
        );

        Assert.Equal(parentCommentId, comment.ParentCommentId);
        Assert.Null(comment.HomePage);
        Assert.NotEqual(comment.ParentCommentId, comment.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithMissingUserName_ThrowsArgumentException(
        string? invalidUserName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Comment.Create(
                userName: invalidUserName!,
                email: "user@example.com",
                homePage: null,
                message: "Test message",
                parentCommentId: null));

        Assert.Equal("userName", exception.ParamName);
    }

    [Theory]
    [InlineData("user_name")]
    [InlineData("user-name")]
    [InlineData("user name")]
    [InlineData("Користувач")]
    [InlineData("user!")]
    public void Create_WithInvalidUserNameCharacters_ThrowsArgumentException(
        string invalidUserName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Comment.Create(
                userName: invalidUserName,
                email: "user@example.com",
                homePage: null,
                message: "Test message",
                parentCommentId: null));

        Assert.Equal("userName", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithMissingEmail_ThrowsArgumentException(
        string? invalidEmail)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Comment.Create(
                userName: "user",
                email: invalidEmail!,
                homePage: null,
                message: "Test message",
                parentCommentId: null));

        Assert.Equal("email", exception.ParamName);
    }

    [Theory]
    [InlineData("user@")]
    [InlineData("@example.com")]
    [InlineData("user@@example.com")]
    public void Create_WithInvalidEmailFormat_ThrowsArgumentException(
        string invalidEmail)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Comment.Create(
                userName: "user",
                email: invalidEmail,
                homePage: null,
                message: "Test message",
                parentCommentId: null));

        Assert.Equal("email", exception.ParamName);
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("non-a-url")]
    public void Create_WithInvalidHomePage_ThrowsArgumentException(
        string invalidHomePage)
    {

        const string email = "userEmail@gmail.com";
        var exception = Assert.Throws<ArgumentException>(() =>
            Comment.Create(
                userName: "user",
                email: email,
                homePage: invalidHomePage,
                message: "Test message",
                parentCommentId: null));

        Assert.Equal("homePage", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithMissingMessage_ThrowsArgumentException(
        string? invalidMessage)
    {
        const string email = "userEmail@gmail.com";
        var exception = Assert.Throws<ArgumentException>(() =>
            Comment.Create(
                userName: "user",
                email: email,
                homePage: null,
                message: invalidMessage!,
                parentCommentId: null));

        Assert.Equal("message", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyParentCommentId_ThrowsArgumentException()
    {
        const string username = "username";
        const string email = "userEmail@gmail.com";
        const string message = "message";

        var exception = Assert.Throws<ArgumentException>(() =>
            Comment.Create(
                userName: username,
                email: email,
                homePage: null,
                message: message,
                parentCommentId: Guid.Empty));

        Assert.Equal("parentCommentId", exception.ParamName);
    }
}
