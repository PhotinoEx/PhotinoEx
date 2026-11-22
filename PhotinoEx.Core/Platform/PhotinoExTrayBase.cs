using System.Collections.Concurrent;
using PhotinoEx.Core.Models;

namespace PhotinoEx.Core.Platform;

internal abstract class PhotinoExTrayBase : IPhotinoExTray
{
    private readonly ConcurrentDictionary<string, IPhotinoExTrayIcon> _icons = [];

    public event EventHandler<Exception>? UnhandledException;

    public async Task<IPhotinoExTrayIcon> RegisterAsync(
        TrayIconOptions options,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);

        if (_icons.ContainsKey(options.Id))
        {
            throw new InvalidOperationException($"Tray icon ID '{options.Id}' is already registered.");
        }

        var icon = await CreateIconAsync(options, cancellationToken).ConfigureAwait(false);
        if (!_icons.TryAdd(options.Id, icon))
        {
            await icon.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"Tray icon ID '{options.Id}' is already registered.");
        }

        return icon;
    }

    public bool TryGet(string id, out IPhotinoExTrayIcon? icon) => _icons.TryGetValue(id, out icon);

    public async Task<bool> UnregisterAsync(string id)
    {
        if (!_icons.TryRemove(id, out var icon))
        {
            return false;
        }

        await icon.DisposeAsync().ConfigureAwait(false);
        return true;
    }

    public async Task UnregisterAllAsync()
    {
        foreach (var id in _icons.Keys)
        {
            await UnregisterAsync(id).ConfigureAwait(false);
        }
    }

    internal void ReportUnhandledException(Exception exception) => UnhandledException?.Invoke(this, exception);

    protected abstract Task<IPhotinoExTrayIcon> CreateIconAsync(
        TrayIconOptions options,
        CancellationToken cancellationToken
    );

    private static void Validate(TrayIconOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Id))
        {
            throw new ArgumentException("A tray icon ID is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.IconPath))
        {
            throw new ArgumentException("A tray icon path is required.", nameof(options));
        }

        if (!File.Exists(options.IconPath))
        {
            throw new FileNotFoundException("The tray icon file does not exist.", options.IconPath);
        }

        options.Menu?.Validate();
    }
}
