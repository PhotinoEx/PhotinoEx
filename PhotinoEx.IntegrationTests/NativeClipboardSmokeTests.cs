using System.Runtime.InteropServices;
using PhotinoEx.Core;
using Xunit;

namespace PhotinoEx.IntegrationTests;

public sealed class NativeClipboardSmokeTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task NativeClipboardPublishesTextAndMixedPaths()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            return;
        }

        var directory = Directory.CreateTempSubdirectory("PhotinoEx ünicode # ");
        var file = Path.Combine(directory.FullName, "empty file #.txt");
        File.Create(file).Dispose();
        var text = $"PhotinoEx clipboard ünicode # {Guid.NewGuid():N}";

        try
        {
            await NativeWindowHarness.RunAsync(async window =>
            {
                window.CopyTextToClipboard(text);
                Assert.Equal(text, await ReadTextAsync(window));

                window.CopyFilesToClipboard([file, directory.FullName]);
                if (OperatingSystem.IsWindows())
                {
                    var copiedPaths = WindowsClipboard.ReadFiles();
                    Assert.Contains(Path.GetFullPath(file), copiedPaths);
                    Assert.Contains(Path.GetFullPath(directory.FullName), copiedPaths);
                }
                else
                {
                    Assert.True(await LinuxClipboard.WaitForMimeTypeAsync(window, "text/uri-list"));
                }
            });
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static Task<string> ReadTextAsync(PhotinoExWindow window) => OperatingSystem.IsWindows()
        ? Task.FromResult(WindowsClipboard.ReadText())
        : LinuxClipboard.ReadTextAsync(window);

    private static class LinuxClipboard
    {
        private static Gdk.Clipboard? _clipboard;

        public static async Task<string> ReadTextAsync(PhotinoExWindow window)
        {
            for (var attempt = 0; attempt < 50; attempt++)
            {
                try
                {
                    Task<string?>? read = null;
                    window.Invoke(() => read = GetClipboard().ReadTextAsync());
                    return await read! ?? string.Empty;
                }
                catch (GLib.GException) when (attempt < 49)
                {
                    await Task.Delay(20);
                }
            }

            throw new InvalidOperationException("The clipboard did not publish text.");
        }

        public static async Task<bool> WaitForMimeTypeAsync(PhotinoExWindow window, string mimeType)
        {
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var containsMimeType = false;
                window.Invoke(() => containsMimeType = GetClipboard().GetFormats().ContainMimeType(mimeType));
                if (containsMimeType)
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return false;
        }

        private static Gdk.Clipboard GetClipboard() =>
            _clipboard ??= Gdk.Display.GetDefault()?.GetClipboard()
            ?? throw new InvalidOperationException("The default GDK display is unavailable.");
    }

    private static class WindowsClipboard
    {
        private const uint CfUnicodeText = 13;
        private const uint CfHDrop = 15;

        public static string ReadText() => WithClipboard(() =>
        {
            var memory = GetClipboardData(CfUnicodeText);
            var pointer = GlobalLock(memory);
            try
            {
                return Marshal.PtrToStringUni(pointer) ?? string.Empty;
            }
            finally
            {
                GlobalUnlock(memory);
            }
        });

        public static IReadOnlyList<string> ReadFiles() => WithClipboard(() =>
        {
            var drop = GetClipboardData(CfHDrop);
            var count = DragQueryFileW(drop, uint.MaxValue, null, 0);
            var paths = new List<string>((int)count);
            for (uint index = 0; index < count; index++)
            {
                var length = DragQueryFileW(drop, index, null, 0);
                var buffer = new char[length + 1];
                DragQueryFileW(drop, index, buffer, (uint)buffer.Length);
                paths.Add(new string(buffer, 0, (int)length));
            }

            return paths;
        });

        private static T WithClipboard<T>(Func<T> read)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                throw new InvalidOperationException($"OpenClipboard failed with error {Marshal.GetLastWin32Error()}.");
            }

            try
            {
                return read();
            }
            finally
            {
                CloseClipboard();
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr owner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint format);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr memory);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr memory);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint DragQueryFileW(IntPtr drop, uint index, [Out] char[]? path, uint length);
    }
}
