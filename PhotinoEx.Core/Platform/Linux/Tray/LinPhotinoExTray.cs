using System.Collections.Concurrent;

namespace PhotinoEx.Core.Platform.Linux.Tray;

public class LinPhotinoExTray : IPhotinoExTray
{
    private ConcurrentDictionary<string, IPhotinoExTrayIcon> _iconList { get; set; } = new();
    private IntPtr _busConnection { get; set; }
    private int _instanceCount { get; set; }

    public LinPhotinoExTray(IntPtr connection)
    {
        _busConnection = connection;
    }

    public Task<IPhotinoExTrayIcon> CreateTrayIconAsync(string id, string iconPath, string? toolTip = null, object? menu = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"Argument: {nameof(id)} cant be null or whitespace, must be a unique identifier");
        }

        if (_iconList.ContainsKey(id))
        {
            throw new InvalidOperationException($"id: {id} already exists, must be a unique identifier");
        }

        var trayIcon = new LinPhotinoExTrayIcon(_busConnection, id, iconPath, toolTip, menu, ++_instanceCount);

        _iconList.TryAdd(id, trayIcon);
        Console.WriteLine($"Created and added {id} to tray");

        return Task.FromResult<IPhotinoExTrayIcon>(trayIcon);
    }

    public bool TryGetTrayIcon(string id, out IPhotinoExTrayIcon? icon)
    {
        return _iconList.TryGetValue(id, out icon);
    }

    public bool TryRemoveTrayIcon(string id, out IPhotinoExTrayIcon? icon)
    {
        return _iconList.TryRemove(id, out icon);
    }

    public bool TryRemoveAllTrayIcons()
    {
        _iconList.Clear();
        return !_iconList.Any();
    }
}
