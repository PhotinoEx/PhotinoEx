using PhotinoEx.Core.Models;

namespace PhotinoEx.Core.Platform.Linux.Tray;

internal sealed class LinPhotinoExTray : PhotinoExTrayBase
{
    private int _instanceCount;

    protected override async Task<IPhotinoExTrayIcon> CreateIconAsync(
        TrayIconOptions options,
        CancellationToken cancellationToken
    )
    {
        var icon = new LinPhotinoExTrayIcon(this, options, Interlocked.Increment(ref _instanceCount));
        try
        {
            await icon.RegisterAsync(cancellationToken).ConfigureAwait(false);
            return icon;
        }
        catch (Exception exception)
        {
            await icon.DisposeAsync().ConfigureAwait(false);
            throw new TrayIconException($"Could not register Linux tray icon '{options.Id}'.", exception);
        }
    }
}
