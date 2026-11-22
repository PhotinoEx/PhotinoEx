using PhotinoEx.Blazor;
using PhotinoEx.Core;
using Xunit;

namespace PhotinoEx.Core.Tests;

public sealed class PhotinoExClipboardTests
{
    [Fact]
    public async Task CopyTextRequiresAnInitializedWindow()
    {
        var clipboard = new PhotinoExClipboard(new PhotinoExWindow());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await clipboard.CopyTextAsync("copied text", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task CopyTextRejectsNull()
    {
        var clipboard = new PhotinoExClipboard(new PhotinoExWindow());

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await clipboard.CopyTextAsync(null!, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task CopyFilesAcceptsFilesAndDirectories()
    {
        var directory = Directory.CreateTempSubdirectory();
        var file = Path.Combine(directory.FullName, "file.txt");
        await File.WriteAllTextAsync(file, "content", TestContext.Current.CancellationToken);
        var clipboard = new PhotinoExClipboard(new PhotinoExWindow());

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await clipboard.CopyFilesAsync(
                    [file, directory.FullName],
                    TestContext.Current.CancellationToken
                )
            );
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task CopyFilesRejectsEmptyList()
    {
        var clipboard = new PhotinoExClipboard(new PhotinoExWindow());

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await clipboard.CopyFilesAsync([], TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task CopyFilesRejectsMissingPaths()
    {
        var clipboard = new PhotinoExClipboard(new PhotinoExWindow());

        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await clipboard.CopyFilesAsync(
                [Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}")],
                TestContext.Current.CancellationToken
            )
        );
    }
}
