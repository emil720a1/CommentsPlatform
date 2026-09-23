using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using CommentsPlatform.Domain;
using CommentsPlatform.Infrastructure.Persistence;
using CommentsPlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace CommentsPlatform.Infrastructure.IntegrationTests.Persistence.Repositories;

public sealed class CommentRepositoryTests : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer;

    private ApplicationDbContext _dbContext = null!;
    private CommentRepository _commentRepository = null!;

    public CommentRepositoryTests()
    {
        _dbContainer = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_dbContainer.GetConnectionString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        await _dbContext.Database.MigrateAsync();

        _commentRepository = new CommentRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
        }

        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetTopLevelCommentsAsync_WhenDatabaseIsEmpty_ReturnsEmptyPage()
    {
        var parameters = new GetCommentsParameters(
            1,
            10,
            CommentSortBy.CreatedAt,
            SortDirection.Descending);

        var result = await _commentRepository.GetTopLevelCommentsAsync(parameters, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task GetTopLevelCommentsAsync_WhenDatabaseContainsReplies_ReturnsOnlyTopLevelComments()
    {
        var firstTopLevelComment = CreateTopLevelComment("user1", "First message");
        var secondTopLevelComment = CreateTopLevelComment("user2", "Second message");
        var reply = Comment.Create(
            "replyUser",
            "reply@example.com",
            null,
            "Reply message",
            firstTopLevelComment.Id);

        _dbContext.Comments.AddRange(
            firstTopLevelComment,
            secondTopLevelComment,
            reply);
        await _dbContext.SaveChangesAsync();

        var parameters = new GetCommentsParameters(
            1,
            10,
            CommentSortBy.CreatedAt,
            SortDirection.Descending);

        var result = await _commentRepository.GetTopLevelCommentsAsync(
            parameters,
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, item => item.Id == firstTopLevelComment.Id);
        Assert.Contains(result.Items, item => item.Id == secondTopLevelComment.Id);
        Assert.DoesNotContain(result.Items, item => item.Id == reply.Id);
    }

    [Fact]
    public async Task GetTopLevelCommentsAsync_WhenMultiplePagesExist_ReturnsRequestedPageAndTotalCount()
    {
        var comments = Enumerable.Range(1, 5)
            .Select(index => CreateTopLevelComment(
                $"user{index}",
                $"Message {index}"))
            .ToArray();

        _dbContext.Comments.AddRange(comments);
        await _dbContext.SaveChangesAsync();

        var parameters = new GetCommentsParameters(
            2,
            2,
            CommentSortBy.CreatedAt,
            SortDirection.Descending);

        var result = await _commentRepository.GetTopLevelCommentsAsync(
            parameters,
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetTopLevelCommentsAsync_WithRequestedDirection_SortsByCreatedAt()
    {
        var olderComment = CreateTopLevelComment("olderUser", "Older message");
        await Task.Delay(TimeSpan.FromMilliseconds(10));
        var newerComment = CreateTopLevelComment("newerUser", "Newer message");

        _dbContext.Comments.AddRange(olderComment, newerComment);
        await _dbContext.SaveChangesAsync();

        var ascendingParameters = new GetCommentsParameters(
            1,
            10,
            CommentSortBy.CreatedAt,
            SortDirection.Ascending);
        var descendingParameters = ascendingParameters with
        {
            SortDirection = SortDirection.Descending
        };

        var ascendingResult = await _commentRepository.GetTopLevelCommentsAsync(
            ascendingParameters,
            CancellationToken.None);
        var descendingResult = await _commentRepository.GetTopLevelCommentsAsync(
            descendingParameters,
            CancellationToken.None);

        Assert.Equal(
            new[] { olderComment.Id, newerComment.Id },
            ascendingResult.Items.Select(item => item.Id).ToArray());
        Assert.Equal(
            new[] { newerComment.Id, olderComment.Id },
            descendingResult.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task GetTopLevelCommentsAsync_WithUnsupportedSorting_ThrowsArgumentOutOfRangeException()
    {
        var invalidSortFieldParameters = new GetCommentsParameters(
            1,
            10,
            (CommentSortBy)999,
            SortDirection.Descending);
        var invalidSortDirectionParameters = new GetCommentsParameters(
            1,
            10,
            CommentSortBy.CreatedAt,
            (SortDirection)999);

        var sortFieldException = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _commentRepository.GetTopLevelCommentsAsync(
                invalidSortFieldParameters,
                CancellationToken.None));
        var sortDirectionException = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _commentRepository.GetTopLevelCommentsAsync(
                invalidSortDirectionParameters,
                CancellationToken.None));

        Assert.Equal("SortBy", sortFieldException.ParamName);
        Assert.Equal("SortDirection", sortDirectionException.ParamName);
    }

    [Fact]
    public async Task GetTopLevelCommentsAsync_WhenQueryCompletes_DoesNotTrackComments()
    {
        var comment = CreateTopLevelComment("user1", "Test message");

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var parameters = new GetCommentsParameters(
            1,
            10,
            CommentSortBy.CreatedAt,
            SortDirection.Descending);

        var result = await _commentRepository.GetTopLevelCommentsAsync(
            parameters,
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Empty(_dbContext.ChangeTracker.Entries<Comment>());
    }

    private static Comment CreateTopLevelComment(
        string userName,
        string message)
    {
        return Comment.Create(
            userName,
            $"{userName}@example.com",
            null,
            message,
            null);
    }
}
