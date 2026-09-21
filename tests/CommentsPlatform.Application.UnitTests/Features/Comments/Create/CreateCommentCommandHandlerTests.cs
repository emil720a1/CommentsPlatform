using FluentValidation;
using FluentValidation.Results;
using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Features.Comments.Create;
using CommentsPlatform.Domain;
using ErrorOr;
using Moq;

namespace CommentsPlatform.Application.UnitTests.Features.Comments.Create;

public sealed class CreateCommentCommandHandlerTests
{
    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly CreateCommentCommandHandler _handler;

    public CreateCommentCommandHandlerTests()
    {
        _commentRepositoryMock = new Mock<ICommentRepository>();

        var validator = new CreateCommentCommandValidator();

        _handler = new CreateCommentCommandHandler(
            _commentRepositoryMock.Object,
            validator);
    }

    [Fact]
    public async Task Handle_WithValidTopLevelComment_ReturnsCommentIdAndPersistsComment()
    {
        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            null);

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

        _commentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Comment>(comment =>
                    comment.Id == result.Value &&
                    comment.UserName == command.UserName &&
                    comment.Email == command.Email &&
                    comment.HomePage == command.HomePage &&
                    comment.Message == command.Message &&
                    comment.ParentCommentId == command.ParentCommentId),
                CancellationToken.None),
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
            parentCommentId);

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
    public async Task Handle_WithInvalidCommand_ReturnsValidationErrorAndDoesNotUseRepository()
    {
        var parentCommentId = Guid.NewGuid();

        var command = new CreateCommentCommand(
            string.Empty,
            "user@example.com",
            "https://example.com/",
            "Test message",
            parentCommentId);

        var result = await _handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Comments.UserName.Required",
            error.Code);

        Assert.Equal(
            ErrorType.Validation,
            error.Type);

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
    public async Task Handle_WithExistingParentComment_ReturnsCommentIdAndPersistsReply()
    {
        var parentCommentId = Guid.NewGuid();

        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            parentCommentId);

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
        var validatorMock = new Mock<IValidator<CreateCommentCommand>>();

        validatorMock
            .Setup(validator => validator.ValidateAsync(
                It.IsAny<CreateCommentCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var handler = new CreateCommentCommandHandler(
            _commentRepositoryMock.Object,
            validatorMock.Object);

        var command = new CreateCommentCommand(
            string.Empty,
            "user@example.com",
            "https://example.com/",
            "Test message",
            null);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);
        var error = Assert.Single(result.Errors);

        Assert.Equal("Comments.DomainValidation", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        validatorMock.Verify(
            validator => validator.ValidateAsync(
                command,
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
    public async Task Handle_WithMultipleValidationFailures_ReturnsAllValidationErrorsAndDoesNotUseRepository()
    {
        var validatorMock = new Mock<IValidator<CreateCommentCommand>>();

        var validationResult = new ValidationResult(
        [
            new ValidationFailure(
                nameof(CreateCommentCommand.UserName),
                "User name is required.")
            {
                ErrorCode = "Comments.UserName.Required"
            },
            new ValidationFailure(
                nameof(CreateCommentCommand.Email),
                "Email is required.")
            {
                ErrorCode = "Comments.Email.Required"
            }
        ]);

        validatorMock
            .Setup(validator => validator.ValidateAsync(
                It.IsAny<CreateCommentCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var handler = new CreateCommentCommandHandler(
            _commentRepositoryMock.Object,
            validatorMock.Object);

        var command = new CreateCommentCommand(
            string.Empty,
            string.Empty,
            null,
            "Test message",
            null);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Collection(
            result.Errors,
            error =>
            {
                Assert.Equal("Comments.UserName.Required", error.Code);
                Assert.Equal(ErrorType.Validation, error.Type);
            },
            error =>
            {
                Assert.Equal("Comments.Email.Required", error.Code);
                Assert.Equal(ErrorType.Validation, error.Type);
            });

        validatorMock.Verify(
            validator => validator.ValidateAsync(
                command,
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
    public async Task Handle_WithValidCommand_ForwardsCancellationToken()
    {
        var validatorMock = new Mock<IValidator<CreateCommentCommand>>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            null);

        validatorMock
            .Setup(validator => validator.ValidateAsync(
                command,
                cancellationToken))
            .ReturnsAsync(new ValidationResult());

        _commentRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Comment>(),
                cancellationToken))
            .Returns(Task.CompletedTask);

        var handler = new CreateCommentCommandHandler(
            _commentRepositoryMock.Object,
            validatorMock.Object);

        var result = await handler.Handle(
            command,
            cancellationToken);

        Assert.False(result.IsError);

        validatorMock.Verify(
            validator => validator.ValidateAsync(
                command,
                cancellationToken),
            Times.Once);

        _commentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Comment>(),
                cancellationToken),
            Times.Once);
    }
}
