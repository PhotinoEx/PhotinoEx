namespace PhotinoEx.Core.Models;

public enum TrayMouseButton
{
    Primary,
    Secondary,
    Middle,
}

public sealed class TrayIconEventArgs : EventArgs
{
    public required TrayMouseButton Button { get; init; }
    public int ClickCount { get; init; }
}
