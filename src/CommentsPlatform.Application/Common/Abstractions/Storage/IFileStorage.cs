namespace CommentsPlatform.Application.Common.Abstractions.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(
        Stream content,
        string storageKey,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken);
}