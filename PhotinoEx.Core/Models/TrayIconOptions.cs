namespace PhotinoEx.Core.Models;

public sealed record TrayIconOptions(
    string Id,
    string IconPath,
    string? ToolTip = null,
    TrayMenu? Menu = null,
    bool IsVisible = true
);
