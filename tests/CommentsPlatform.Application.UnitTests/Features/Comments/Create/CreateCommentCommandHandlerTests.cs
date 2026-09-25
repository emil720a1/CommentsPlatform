using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Common.Abstractions.Security;
using CommentsPlatform.Application.Features.Comments.Create;
using CommentsPlatform.Domain;
using ErrorOr;
using Moq;

namespace CommentsPlatform.Application.UnitTests.Features.Comments.Create;

public sealed class CreateCommentCommandHandlerTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly Mock<ICaptchaValidator> _captchaValidatorMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly CreateCommentCommandHandler _handler;

    public CreateCommentCommandHandlerTests()
    {
        _commentRepositoryMock = new Mock<ICommentRepository>();
        _captchaValidatorMock = new Mock<ICaptchaValidator>();
        _timeProviderMock = new Mock<TimeProvider>();

        _captchaValidatorMock.Setup(validator => validator.IsValidAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _timeProviderMock
            .Setup(timeProvider => timeProvider.GetUtcNow())
            .Returns(FixedUtcNow);

        _handler = new CreateCommentCommandHandler(
            _commentRepositoryMock.Object,
            _captchaValidatorMock.Object,
            _timeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidTopLevelComment_ReturnsCommentIdAndPersistsComment()
    {
        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            null,
            "valid-captcha-token");

        _commentRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Comment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.NotEqual(Guid.Empty, result.Value);

        _captchaValidatorMock.Verify(
            validator => validator.IsValidAsync(
                command.CaptchaToken,
                CancellationToken.None),
            Times.Once);

        _commentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Comment>(comment =>
                    comment.Id == result.Value &&
                    comment.UserName == command.UserName &&
                    comment.Email == command.Email &&
                    comment.HomePage == command.HomePage &&
                    comment.Message == command.Message &&
                    comment.ParentCommentId == command.ParentCommentId &&
                    comment.CreatedAt == FixedUtcNow),
                CancellationToken.None),
            Times.Once);

        _timeProviderMock.Verify(
            timeProvider => timeProvider.GetUtcNow(),
            Times.Once);

        _commentRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingParentComment_ReturnsParentNotFoundAndDoesNotPersist()
    {
        var parentCommentId = Guid.NewGuid();

        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            parentCommentId,
            "valid-captcha-token");

        _commentRepositoryMock
            .Setup(repository => repository.ExistsAsync(
                parentCommentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            CreateCommentErrors.ParentNotFound.Code,
            error.Code);

        Assert.Equal(
            CreateCommentErrors.ParentNotFound.Type,
            error.Type);

        _commentRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                parentCommentId,
                CancellationToken.None),
            Times.Once);

        _commentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Comment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingParentComment_ReturnsCommentIdAndPersistsReply()
    {
        var parentCommentId = Guid.NewGuid();

        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            parentCommentId,
            "valid-captcha-token");

        _commentRepositoryMock.Setup(
            repository => repository.ExistsAsync(
                parentCommentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _commentRepositoryMock.Setup(
            repository => repository.AddAsync(
                It.IsAny<Comment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.NotEqual(Guid.Empty, result.Value);

        _commentRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                parentCommentId,
                CancellationToken.None),
            Times.Once);

        _commentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Comment>(comment =>
                    comment.Id == result.Value &&
                    comment.ParentCommentId == parentCommentId),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDomainFactoryRejectsCommand_ReturnsDomainValidationError()
    {
        var command = new CreateCommentCommand(
            string.Empty,
            "user@example.com",
            "https://example.com/",
            "Test message",
            null,
            "valid-captcha-token");

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);
        var error = Assert.Single(result.Errors);

        Assert.Equal("Comments.DomainValidation", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        _commentRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _commentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Comment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ForwardsCancellationToken()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            null,
            "valid-captcha-token");

        _commentRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Comment>(),
                cancellationToken))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            command,
            cancellationToken);

        Assert.False(result.IsError);

        _captchaValidatorMock.Verify(
            validator => validator.IsValidAsync(
                command.CaptchaToken,
                cancellationToken),
            Times.Once);

        _commentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Comment>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidCaptcha_ReturnsInvalidCaptchaAndDoesNotUseRepository()
    {
        var parentCommentId = Guid.NewGuid();
        const string captchaToken = "invalid-captcha-token";

        _captchaValidatorMock
            .Setup(validator => validator.IsValidAsync(
                captchaToken,
                CancellationToken.None))
            .ReturnsAsync(false);

        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            parentCommentId,
            captchaToken);

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);

        var error = Assert.Single(result.Errors);

        Assert.Equal(CreateCommentErrors.InvalidCaptcha.Code, error.Code);
        Assert.Equal(CreateCommentErrors.InvalidCaptcha.Type, error.Type);

        _captchaValidatorMock.Verify(
            validator => validator.IsValidAsync(
                captchaToken,
                CancellationToken.None),
            Times.Once);

        _commentRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _commentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Comment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidCaptcha_ReturnsBeforeDomainValidation()
    {
        const string captchaToken = "invalid-captcha-token";

        _captchaValidatorMock
            .Setup(validator => validator.IsValidAsync(
                captchaToken,
                CancellationToken.None))
            .ReturnsAsync(false);

        var command = new CreateCommentCommand(
            string.Empty,
            "user@example.com",
            "https://example.com/",
            "Test message",
            null,
            captchaToken);

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);

        var error = Assert.Single(result.Errors);

        Assert.Equal(CreateCommentErrors.InvalidCaptcha.Code, error.Code);
        Assert.NotEqual("Comments.DomainValidation", error.Code);

        _commentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Comment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
