using CommentsPlatform.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace CommentsPlatform.Infrastructure.UnitTests.Storage;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        $"comments-platform-tests-{Guid.NewGuid():N}");

    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _storage = new LocalFileStorage(
            Options.Create(new LocalFileStorageOptions
            {
                RootPath = _rootPath
            }));
    }

    [Fact]
    public async Task SaveAsync_WithValidContent_WritesFileAndReturnsStorageKey()
    {
        byte[] expectedContent = [1, 2, 3, 4];
        const string storageKey = "comments/comment-id/file.png";
        await using var content = new MemoryStream(expectedContent);

        var result = await _storage.SaveAsync(
            content,
            storageKey,
            CancellationToken.None);

        Assert.Equal(storageKey, result);

        var filePath = GetFilePath(storageKey);
        Assert.True(File.Exists(filePath));
        Assert.Equal(expectedContent, await File.ReadAllBytesAsync(filePath));
    }

    [Fact]
    public async Task SaveAsync_WithNestedStorageKey_CreatesDirectories()
    {
        const string storageKey = "comments/first/second/file.txt";
        await using var content = new MemoryStream([1]);

        await _storage.SaveAsync(
            content,
            storageKey,
            CancellationToken.None);

        Assert.True(File.Exists(GetFilePath(storageKey)));
    }

    [Fact]
    public async Task DeleteAsync_WhenFileExists_RemovesFile()
    {
        const string storageKey = "comments/comment-id/file.txt";
        await using var content = new MemoryStream([1, 2, 3]);

        await _storage.SaveAsync(
            content,
            storageKey,
            CancellationToken.None);

        await _storage.DeleteAsync(
            storageKey,
            CancellationToken.None);

        Assert.False(File.Exists(GetFilePath(storageKey)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("../outside.txt")]
    [InlineData("comments/../outside.txt")]
    [InlineData(@"comments\outside.txt")]
    [InlineData("comments//file.txt")]
    [InlineData("comments/./file.txt")]
    public async Task SaveAsync_WithInvalidStorageKey_ThrowsArgumentException(
        string storageKey)
    {
        await using var content = new MemoryStream([1]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.SaveAsync(
                content,
                storageKey,
                CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_WithAbsoluteStorageKey_ThrowsArgumentException()
    {
        await using var content = new MemoryStream([1]);
        var absoluteStorageKey = Path.GetFullPath(
            Path.Combine(_rootPath, "file.txt"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.SaveAsync(
                content,
                absoluteStorageKey,
                CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_WhenFileAlreadyExists_DoesNotOverwriteFile()
    {
        byte[] originalContent = [1, 2, 3];
        byte[] replacementContent = [9, 9, 9];
        const string storageKey = "comments/comment-id/file.png";
        var filePath = GetFilePath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, originalContent);

        await using var content = new MemoryStream(replacementContent);

        await Assert.ThrowsAsync<IOException>(() =>
            _storage.SaveAsync(
                content,
                storageKey,
                CancellationToken.None));

        Assert.Equal(originalContent, await File.ReadAllBytesAsync(filePath));
    }

    [Fact]
    public async Task SaveAsync_WithUnreadableContent_ThrowsArgumentException()
    {
        var content = new MemoryStream([1]);
        await content.DisposeAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _storage.SaveAsync(
                content,
                "comments/comment-id/file.png",
                CancellationToken.None));
    }

    [Fact]
    public async Task SaveAsync_WhenCopyFails_RemovesPartiallyWrittenFile()
    {
        const string storageKey = "comments/comment-id/file.png";
        await using var content = new FailingCopyStream();

        await Assert.ThrowsAsync<IOException>(() =>
            _storage.SaveAsync(
                content,
                storageKey,
                CancellationToken.None));

        Assert.False(File.Exists(GetFilePath(storageKey)));
    }

    [Fact]
    public async Task DeleteAsync_WithCancelledToken_PropagatesCancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _storage.DeleteAsync(
                "comments/comment-id/file.png",
                cancellationTokenSource.Token));
    }

    [Fact]
    public void Constructor_WithMissingRootPath_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new LocalFileStorageOptions
        {
            RootPath = " "
        });

        Assert.Throws<InvalidOperationException>(() =>
            new LocalFileStorage(options));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private string GetFilePath(string storageKey)
    {
        return Path.Combine(
            _rootPath,
            Path.Combine(storageKey.Split('/')));
    }

    private sealed class FailingCopyStream : MemoryStream
    {
        public override async Task CopyToAsync(
            Stream destination,
            int bufferSize,
            CancellationToken cancellationToken)
        {
            await destination.WriteAsync(
                new byte[] { 1, 2, 3 },
                cancellationToken);

            throw new IOException("Simulated copy failure.");
        }
    }
}
