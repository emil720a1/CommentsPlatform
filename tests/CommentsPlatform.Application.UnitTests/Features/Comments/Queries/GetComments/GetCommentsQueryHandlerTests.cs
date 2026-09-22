using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using Moq;
using Xunit;


namespace CommentsPlatform.Application.UnitTests.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQueryHandlerTests
{
    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly GetCommentsQueryHandler _handler;

    public GetCommentsQueryHandlerTests()
    {
        _commentRepositoryMock = new Mock<ICommentRepository>();
        _handler = new GetCommentsQueryHandler(_commentRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ReturnsPaginatedComments()
    {
        // Arrange
        var query = new GetCommentsQuery(
            Page: 1,
            PageSize: 25,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Descending);

        var expectedItems = new List<CommentDto>
        {
            new(Guid.NewGuid(), "user1", null, DateTimeOffset.UtcNow, "user1@example.com", "Hello"),
            new(Guid.NewGuid(), "user2", null, DateTimeOffset.UtcNow, "user2@example.com", "World"),
        };

        var expectedResult = new PaginatedList<CommentDto>(
            expectedItems,
            page: 1,
            pageSize: 25,
            totalCount: 2);

        _commentRepositoryMock
            .Setup(r => r.GetTopLevelCommentsAsync(
                It.IsAny<GetCommentsParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(25, result.Value.PageSize);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(1, result.Value.TotalPages);
    }

    [Fact]
    public async Task Handle_WhenNoCommentsExist_ReturnsEmptyPaginatedList()
    {
        // Arrange
        var query = new GetCommentsQuery(
            Page: 1,
            PageSize: 25,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Descending);

        var emptyResult = new PaginatedList<CommentDto>(
            new List<CommentDto>(),
            page: 1,
            pageSize: 25,
            totalCount: 0);

        _commentRepositoryMock
            .Setup(r => r.GetTopLevelCommentsAsync(
                It.IsAny<GetCommentsParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Fact]
    public async Task Handle_Always_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var query = new GetCommentsQuery(
            Page: 2,
            PageSize: 10,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Ascending);

        _commentRepositoryMock
            .Setup(r => r.GetTopLevelCommentsAsync(
                It.IsAny<GetCommentsParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<CommentDto>(
                new List<CommentDto>(),
                page: 2,
                pageSize: 10,
                totalCount: 0));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _commentRepositoryMock.Verify(
            r => r.GetTopLevelCommentsAsync(
                It.Is<GetCommentsParameters>(p =>
                    p.Page == 2 &&
                    p.PageSize == 10 &&
                    p.SortBy == CommentSortBy.CreatedAt &&
                    p.SortDirection == SortDirection.Ascending),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultiplePages_ReturnsPaginationMetadataCorrectly()
    {
        // Arrange
        var query = new GetCommentsQuery(
            Page: 1,
            PageSize: 5,
            SortBy: CommentSortBy.CreatedAt,
            SortDirection: SortDirection.Descending);

        var items = Enumerable.Range(1, 5)
            .Select(i => new CommentDto(
                Guid.NewGuid(), $"user{i}", null,
                DateTimeOffset.UtcNow, $"user{i}@example.com", $"Message {i}"))
            .ToList();

        // 13 total items, pageSize 5 → 3 pages
        var pagedResult = new PaginatedList<CommentDto>(items, page: 1, pageSize: 5, totalCount: 13);

        _commentRepositoryMock
            .Setup(r => r.GetTopLevelCommentsAsync(
                It.IsAny<GetCommentsParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(5, result.Value.Items.Count);
        Assert.Equal(13, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.True(result.Value.HasNextPage);
        Assert.False(result.Value.HasPreviousPage);
    }
}