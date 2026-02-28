namespace PhotinoEx.Core.Platform;

public interface IPhotinoExTray
{
    public Task<IPhotinoExTrayIcon> CreateTrayIconAsync(string id, string iconPath, string? toolTip = null, object? menu = null);
    public bool TryGetTrayIcon(string id, out IPhotinoExTrayIcon? icon);
    public bool TryRemoveTrayIcon(string id, out IPhotinoExTrayIcon? icon);
    public bool TryRemoveAllTrayIcons();
}
