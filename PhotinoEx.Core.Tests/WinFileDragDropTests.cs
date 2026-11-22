using System.Text;
using PhotinoEx.Core.Platform.Windows.FileDragDrop;
using Xunit;

namespace PhotinoEx.Core.Tests;

public sealed class WinFileDragDropTests
{
    [Fact]
    public void FileDropPayloadContainsUnicodeDropFilesHeaderAndPaths()
    {
        var paths = new[] { @"C:\folder\ünicode file.txt", @"C:\folder\empty" };

        var payload = WinFileDropPayload.Create(paths);

        Assert.Equal(WinFileDropPayload.HeaderSize, BitConverter.ToInt32(payload, 0));
        Assert.Equal(1, BitConverter.ToInt32(payload, 16));
        Assert.Equal(
            string.Join('\0', paths) + "\0\0",
            Encoding.Unicode.GetString(payload, WinFileDropPayload.HeaderSize, payload.Length - WinFileDropPayload.HeaderSize)
        );
    }

    [Fact]
    public void DropSourceTracksMouseReleaseEscapeAndCancellation()
    {
        const int dragDropCancel = 0x00040101;
        const int dragDropDrop = 0x00040100;
        using var cancellation = new CancellationTokenSource();
        var source = new WinDropSource(cancellation.Token);

        Assert.Equal(0, source.QueryContinueDrag(false, 1));
        Assert.Equal(dragDropDrop, source.QueryContinueDrag(false, 0));
        Assert.Equal(dragDropCancel, source.QueryContinueDrag(true, 1));

        cancellation.Cancel();
        Assert.Equal(dragDropCancel, source.QueryContinueDrag(false, 1));
    }
}
