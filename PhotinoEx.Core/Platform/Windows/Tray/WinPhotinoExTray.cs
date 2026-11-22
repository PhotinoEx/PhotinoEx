using PhotinoEx.Core.Models;
using System.Runtime.Versioning;

namespace PhotinoEx.Core.Platform.Windows.Tray;

[SupportedOSPlatform("windows")]
internal sealed class WinPhotinoExTray : PhotinoExTrayBase, IDisposable
{
    private readonly WinTrayNative _native = new();
    private int _nextId;

    protected override async Task<IPhotinoExTrayIcon> CreateIconAsync(
        TrayIconOptions options,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var icon = new WinPhotinoExTrayIcon(this, _native, options, unchecked((uint)Interlocked.Increment(ref _nextId)));
        try
        {
            await icon.RegisterAsync().ConfigureAwait(false);
            return icon;
        }
        catch (Exception exception)
        {
            await icon.DisposeAsync().ConfigureAwait(false);
            throw new TrayIconException($"Could not register Windows tray icon '{options.Id}'.", exception);
        }
    }

    public void Dispose() => _native.Dispose();
}
