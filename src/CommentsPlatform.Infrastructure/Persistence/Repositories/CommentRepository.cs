using CommentsPlatform.Application.Common.Abstractions.Persistence;
using CommentsPlatform.Application.Common.Models;
using CommentsPlatform.Application.Features.Comments.Queries.GetComments;
using CommentsPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommentsPlatform.Infrastructure.Persistence.Repositories;

public sealed class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CommentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(
        Guid commentId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Comments
            .AnyAsync(
                comment => comment.Id == commentId,
                cancellationToken);
    }

    public async Task AddAsync(
        Comment comment,
        CancellationToken cancellationToken)
    {
        await _dbContext.Comments.AddAsync(comment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaginatedList<CommentDto>> GetTopLevelCommentsAsync(
        GetCommentsParameters parameters,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Comments
            .AsNoTracking()
            .Where(comment => comment.ParentCommentId == null);

        var totalCount = await query.CountAsync(cancellationToken);

        var orderedQuery = parameters.SortBy switch
        {
            CommentSortBy.CreatedAt => parameters.SortDirection switch
            {
                SortDirection.Ascending => query
                    .OrderBy(comment => comment.CreatedAt)
                    .ThenBy(comment => comment.Id),

                SortDirection.Descending => query
                    .OrderByDescending(comment => comment.CreatedAt)
                    .ThenByDescending(comment => comment.Id),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(parameters.SortDirection),
                    parameters.SortDirection,
                    "Unsupported sort direction.")
            },

            _ => throw new ArgumentOutOfRangeException(
                nameof(parameters.SortBy),
                parameters.SortBy,
                "Unsupported comment sort field.")
        };

        var items = await orderedQuery
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(comment => new CommentDto(
                comment.Id,
                comment.UserName,
                comment.HomePage,
                comment.CreatedAt,
                comment.Email,
                comment.Message))
            .ToListAsync(cancellationToken);

        return new PaginatedList<CommentDto>(
            items,
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }
}
