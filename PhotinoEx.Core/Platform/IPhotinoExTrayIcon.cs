using PhotinoEx.Core.Models;

namespace PhotinoEx.Core.Platform;

public interface IPhotinoExTrayIcon : IAsyncDisposable
{
    string Id { get; }
    bool IsVisible { get; }

    event EventHandler<TrayIconEventArgs>? Clicked;
    event EventHandler? MenuOpening;

    Task SetIconAsync(string iconPath);
    Task SetToolTipAsync(string? toolTip);
    Task SetVisibleAsync(bool visible);
    Task SetMenuAsync(TrayMenu? menu);
}
