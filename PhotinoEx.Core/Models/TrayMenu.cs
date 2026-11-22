namespace PhotinoEx.Core.Models;

public sealed class TrayMenu
{
    public IReadOnlyList<TrayMenuItem> Items { get; init; } = [];

    internal void Validate()
    {
        HashSet<string> ids = [];
        Validate(Items, ids);
    }

    private static void Validate(IEnumerable<TrayMenuItem> items, HashSet<string> ids)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                throw new ArgumentException("Tray menu item IDs cannot be blank.");
            }

            if (!ids.Add(item.Id))
            {
                throw new ArgumentException($"Tray menu item ID '{item.Id}' is duplicated.");
            }

            if (item is TrayMenuCommand command && string.IsNullOrWhiteSpace(command.Text))
            {
                throw new ArgumentException($"Tray menu command '{item.Id}' must have text.");
            }

            if (item is TraySubmenu submenu)
            {
                if (string.IsNullOrWhiteSpace(submenu.Text))
                {
                    throw new ArgumentException($"Tray submenu '{item.Id}' must have text.");
                }

                Validate(submenu.Items, ids);
            }
        }
    }
}

public abstract record TrayMenuItem(string Id);

public sealed record TrayMenuItemState(
    bool IsVisible = true,
    bool IsEnabled = true,
    bool IsChecked = false
);

public sealed record TrayMenuCommand(
    string Id,
    string Text,
    Func<CancellationToken, Task> Activated,
    bool IsEnabled = true,
    bool IsChecked = false,
    Func<CancellationToken, ValueTask<TrayMenuItemState>>? GetState = null
) : TrayMenuItem(Id);

public sealed record TrayMenuSeparator(string Id) : TrayMenuItem(Id);

public sealed record TraySubmenu(
    string Id,
    string Text,
    IReadOnlyList<TrayMenuItem> Items,
    bool IsEnabled = true
) : TrayMenuItem(Id);
