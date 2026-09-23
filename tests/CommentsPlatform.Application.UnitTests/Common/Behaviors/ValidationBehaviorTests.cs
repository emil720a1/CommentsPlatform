using CommentsPlatform.Application.Common.Behaviors;
using CommentsPlatform.Application.Features.Comments.Create;
using ErrorOr;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace CommentsPlatform.Application.UnitTests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithoutValidators_InvokesNextAndReturnsItsResult()
    {
        IValidator<CreateCommentCommand>[] validators = [];

        var behavior =
            new ValidationBehavior<CreateCommentCommand, ErrorOr<Guid>>(
                validators);

        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            null);

        var expectedId = Guid.NewGuid();
        var nextWasCalled = false;

        Task<ErrorOr<Guid>> Next(CancellationToken _)
        {
            nextWasCalled = true;

            return Task.FromResult<ErrorOr<Guid>>(expectedId);
        }

        var result = await behavior.Handle(
            command,
            Next,
            CancellationToken.None);

        Assert.True(nextWasCalled);
        Assert.False(result.IsError);
        Assert.Equal(expectedId, result.Value);
    }

    [Fact]
    public async Task Handle_WithValidValidator_InvokesNextAndReturnsItsResult()
    {
        var validatorMock =
            new Mock<IValidator<CreateCommentCommand>>();

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        validatorMock
            .Setup(validator => validator.ValidateAsync(
                It.IsAny<ValidationContext<CreateCommentCommand>>(),
                cancellationToken))
            .ReturnsAsync(new ValidationResult());

        IValidator<CreateCommentCommand>[] validators =
        [
            validatorMock.Object
        ];

        var behavior =
            new ValidationBehavior<CreateCommentCommand, ErrorOr<Guid>>(
                validators);

        var command = new CreateCommentCommand(
            "user123",
            "user@example.com",
            "https://example.com/",
            "Test message",
            null);

        var expectedId = Guid.NewGuid();
        var nextWasCalled = false;

        Task<ErrorOr<Guid>> Next(CancellationToken _)
        {
            nextWasCalled = true;

            return Task.FromResult<ErrorOr<Guid>>(expectedId);
        }

        var result = await behavior.Handle(
            command,
            Next,
            cancellationToken);

        Assert.True(nextWasCalled);
        Assert.False(result.IsError);
        Assert.Equal(expectedId, result.Value);

        validatorMock.Verify(
            validator => validator.ValidateAsync(
                It.Is<ValidationContext<CreateCommentCommand>>(
                    context => ReferenceEquals(
                        context.InstanceToValidate,
                        command)),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidationFailures_ReturnsAllErrorsAndDoesNotInvokeNext()
    {
        var validatorMock =
            new Mock<IValidator<CreateCommentCommand>>();

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
                It.IsAny<ValidationContext<CreateCommentCommand>>(),
                CancellationToken.None))
            .ReturnsAsync(validationResult);

        IValidator<CreateCommentCommand>[] validators =
        [
            validatorMock.Object
        ];

        var behavior =
            new ValidationBehavior<CreateCommentCommand, ErrorOr<Guid>>(
                validators);

        var command = new CreateCommentCommand(
            string.Empty,
            string.Empty,
            null,
            "Test message",
            null);

        var nextWasCalled = false;

        Task<ErrorOr<Guid>> Next(CancellationToken _)
        {
            nextWasCalled = true;

            return Task.FromResult<ErrorOr<Guid>>(Guid.NewGuid());
        }

        var result = await behavior.Handle(
            command,
            Next,
            CancellationToken.None);

        Assert.False(nextWasCalled);
        Assert.True(result.IsError);
        Assert.Collection(
            result.Errors,
            error =>
            {
                Assert.Equal("Comments.UserName.Required", error.Code);
                Assert.Equal("User name is required.", error.Description);
                Assert.Equal(ErrorType.Validation, error.Type);
            },
            error =>
            {
                Assert.Equal("Comments.Email.Required", error.Code);
                Assert.Equal("Email is required.", error.Description);
                Assert.Equal(ErrorType.Validation, error.Type);
            });

        validatorMock.Verify(
            validator => validator.ValidateAsync(
                It.Is<ValidationContext<CreateCommentCommand>>(
                    context => ReferenceEquals(
                        context.InstanceToValidate,
                        command)),
                CancellationToken.None),
            Times.Once);
    }
}
