using CommentsPlatform.Application.Common.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace CommentsPlatform.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(
        IOptions<LocalFileStorageOptions> options)
    {
        if (string.IsNullOrWhiteSpace(options.Value.RootPath))
        {
            throw new InvalidOperationException(
                "File storage root path is required.");
        }

        _rootPath = Path.GetFullPath(options.Value.RootPath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string storageKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "The content stream must be readable.",
                nameof(content));
        }

        var filePath = GetSafeFilePath(storageKey);
        var directory = Path.GetDirectoryName(filePath)!;

        Directory.CreateDirectory(directory);

        var fileCreated = false;

        try
        {
            await using var fileStream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            fileCreated = true;

            await content.CopyToAsync(
                fileStream,
                cancellationToken);

            return storageKey;
        }
        catch
        {
            if (fileCreated)
            {
                TryDelete(filePath);
            }

            throw;
        }
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = GetSafeFilePath(storageKey);

        File.Delete(filePath);

        return Task.CompletedTask;
    }

    private string GetSafeFilePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException(
                "Storage key is required.",
                nameof(storageKey));
        }

        if (Path.IsPathRooted(storageKey) ||
            storageKey.Contains('\\'))
        {
            throw new ArgumentException(
                "Storage key is invalid.",
                nameof(storageKey));
        }

        var segments = storageKey.Split('/');

        if (segments.Any(segment =>
                string.IsNullOrWhiteSpace(segment) ||
                segment is "." or ".."))
        {
            throw new ArgumentException(
                "Storage key is invalid.",
                nameof(storageKey));
        }

        var relativePath = Path.Combine(segments);
        var fullPath = Path.GetFullPath(
            Path.Combine(_rootPath, relativePath));

        var rootPrefix = _rootPath.TrimEnd(
                             Path.DirectorySeparatorChar,
                             Path.AltDirectorySeparatorChar)
                         + Path.DirectorySeparatorChar;

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!fullPath.StartsWith(rootPrefix, comparison))
        {
            throw new ArgumentException(
                "Storage key points outside the storage root.",
                nameof(storageKey));
        }

        return fullPath;
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // Preserve the original storage exception.
        }
    }
}
