using Gio;

namespace PhotinoEx.Core.Platform.Linux.Tray;

public class LinPhotinoExTrayIcon : IPhotinoExTrayIcon
{
    private DBusConnection _connection { get; set; }
    private string _id { get; set; }
    private string _iconPath { get; set; }
    private string? _toolTip { get; set; }
    private object? _contextMenu { get; set; }
    private bool _isVisible { get; set; } = true;
    private string _busName { get; set; }

    public LinPhotinoExTrayIcon(DBusConnection connection, string id, string iconPath, string? toolTip, object? menu, int instance)
    {
        _connection = connection;
        _id = id;
        _iconPath = iconPath;
        _toolTip = toolTip;
        _contextMenu = menu;
        _busName = $"org.kde.StatusNotifierItem-{Environment.ProcessId}-{instance}";
    }

    public async Task<IPhotinoExTrayIcon> CreateAsync()
    {
        return this;
    }

    public async Task<bool> DisposeAsync()
    {
        return true;
    }

    public void SetVisibility(bool state)
    {
        _isVisible = state;
    }

    public void SetIconPath(string path)
    {
        _iconPath = path;
    }

    public void SetContextMenu(object menu)
    {
        _contextMenu = menu;
    }
}
