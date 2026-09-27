using System.Linq.Expressions;
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

    public async Task<Comment?> GetByIdAsync(
        Guid commentId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Comments
            .Include(comment => comment.Attachments)
            .SingleOrDefaultAsync(
                comment => comment.Id == commentId,
                cancellationToken);
    }

    public Task<Attachment?> GetAttachmentAsync(
        Guid commentId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Attachments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                attachment => attachment.Id == attachmentId &&
                              attachment.CommentId == commentId,
                cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
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
            CommentSortBy.UserName => ApplyOrdering(
                query,
                parameters.SortDirection,
                comment => comment.UserName),

            CommentSortBy.Email => ApplyOrdering(
                query,
                parameters.SortDirection,
                comment => comment.Email),

            CommentSortBy.CreatedAt => ApplyOrdering(
                query,
                parameters.SortDirection,
                comment => comment.CreatedAt),

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
                comment.Message,
                comment.Attachments
                    .OrderBy(attachment => attachment.CreatedAt)
                    .ThenBy(attachment => attachment.Id)
                    .Select(attachment => new AttachmentDto(
                        attachment.Id,
                        attachment.OriginalFileName,
                        attachment.ContentType,
                        attachment.FileSizeBytes,
                        attachment.Width,
                        attachment.Height,
                        attachment.CreatedAt))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PaginatedList<CommentDto>(
            items,
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }

    private static IOrderedQueryable<Comment> ApplyOrdering<TKey>(
        IQueryable<Comment> query,
        SortDirection sortDirection,
        Expression<Func<Comment, TKey>> keySelector)
    {
        return sortDirection switch
        {
            SortDirection.Ascending => query
                .OrderBy(keySelector)
                .ThenBy(comment => comment.Id),
            SortDirection.Descending => query
                .OrderByDescending(keySelector)
                .ThenByDescending(comment => comment.Id),
            _ => throw new ArgumentOutOfRangeException(
                nameof(GetCommentsParameters.SortDirection),
                sortDirection,
                "Unsupported sort direction.")
        };
    }
}
