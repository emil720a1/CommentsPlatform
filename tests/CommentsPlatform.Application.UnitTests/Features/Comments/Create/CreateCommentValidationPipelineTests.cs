using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Features.Comments.Create;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CommentsPlatform.Application.UnitTests.Features.Comments.Create;

public sealed class CreateCommentValidationPipelineTests
{
    [Fact]
    public async Task Send_WithInvalidCommand_ReturnsValidationErrorAndDoesNotUseRepository()
    {
        var commentRepositoryMock = new Mock<ICommentRepository>();

        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory>(
            NullLoggerFactory.Instance);
        services.AddApplication();
        services.AddSingleton(commentRepositoryMock.Object);

        await using var serviceProvider = services.BuildServiceProvider();

        var sender = serviceProvider.GetRequiredService<ISender>();

        var command = new CreateCommentCommand(
            string.Empty,
            "user@example.com",
            "https://example.com/",
            "Test message",
            null,
            "valid-captcha-token");

        var result = await sender.Send(
            command,
            CancellationToken.None);

        Assert.True(result.IsError);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Comments.UserName.Required", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        commentRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        commentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Domain.Comment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
