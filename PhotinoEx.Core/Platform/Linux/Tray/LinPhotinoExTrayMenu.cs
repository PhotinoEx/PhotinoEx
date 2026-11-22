using PhotinoEx.Core.Models;
using Tmds.DBus;

namespace PhotinoEx.Core.Platform.Linux.Tray;

[DBusInterface(
    "com.canonical.dbusmenu",
    GetPropertyMethod = nameof(IDBusMenu.GetDBusPropertyAsync),
    GetAllPropertiesMethod = nameof(IDBusMenu.GetAllPropertiesAsync),
    WatchPropertiesMethod = nameof(IDBusMenu.WatchPropertiesAsync)
)]
public interface IDBusMenu : IDBusObject
{
    Task<object> GetDBusPropertyAsync(string property);
    Task<IDictionary<string, object>> GetAllPropertiesAsync();
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    Task<(uint Revision, DBusMenuLayout Layout)> GetLayoutAsync(int parentId, int recursionDepth, string[] propertyNames);
    Task<DBusMenuProperties[]> GetGroupPropertiesAsync(int[] ids, string[] propertyNames);
    Task<object> GetPropertyAsync(int id, string name);
    Task EventAsync(int id, string eventId, object data, uint timestamp);
    Task<int[]> EventGroupAsync(DBusMenuEvent[] events);
    Task<bool> AboutToShowAsync(int id);
    Task<(int[] UpdatesNeeded, int[] IdErrors)> AboutToShowGroupAsync(int[] ids);
    Task<IDisposable> WatchLayoutUpdatedAsync(Action<(uint Revision, int Parent)> handler);
    Task<IDisposable> WatchItemsPropertiesUpdatedAsync(
        Action<(DBusMenuProperties[] Updated, DBusMenuRemovedProperties[] Removed)> handler
    );
}

public struct DBusMenuLayout
{
    public int Id;
    public IDictionary<string, object> Properties;
    public object[] Children;
}

public struct DBusMenuEvent
{
    public int Id;
    public string EventId;
    public object Data;
    public uint Timestamp;
}

public struct DBusMenuProperties
{
    public int Id;
    public IDictionary<string, object> Properties;
}

public struct DBusMenuRemovedProperties
{
    public int Id;
    public string[] Properties;
}

internal sealed class LinPhotinoExTrayMenu : IDBusMenu
{
    internal static readonly ObjectPath MenuPath = new("/MenuBar");
    private readonly LinPhotinoExTray _owner;
    private readonly LinPhotinoExTrayIcon _icon;
    private Dictionary<int, TrayMenuItem> _items = [];
    private Dictionary<int, TrayMenuItemState> _states = [];
    private TrayMenu? _menu;
    private uint _revision = 1;

    internal LinPhotinoExTrayMenu(LinPhotinoExTray owner, LinPhotinoExTrayIcon icon, TrayMenu? menu)
    {
        _owner = owner;
        _icon = icon;
        SetMenu(menu, false);
    }

    public ObjectPath ObjectPath => MenuPath;
    public event Action<PropertyChanges>? OnPropertiesChanged;
    public event Action<(uint Revision, int Parent)>? OnLayoutUpdated;
    public event Action<(DBusMenuProperties[] Updated, DBusMenuRemovedProperties[] Removed)>? OnItemsPropertiesUpdated;

    internal void SetMenu(TrayMenu? menu) => SetMenu(menu, true);

    public Task<object> GetDBusPropertyAsync(string property) => property switch
    {
        "Version" => Task.FromResult<object>((uint)4),
        "TextDirection" => Task.FromResult<object>("ltr"),
        "Status" => Task.FromResult<object>("normal"),
        "IconThemePath" => Task.FromResult<object>(Array.Empty<string>()),
        _ => throw new ArgumentException($"Unknown D-Bus menu property '{property}'.", nameof(property)),
    };

    public Task<IDictionary<string, object>> GetAllPropertiesAsync() => Task.FromResult<IDictionary<string, object>>(
        new Dictionary<string, object>
        {
            ["Version"] = (uint)4,
            ["TextDirection"] = "ltr",
            ["Status"] = "normal",
            ["IconThemePath"] = Array.Empty<string>(),
        }
    );

    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler) =>
        SignalWatcher.AddAsync(this, nameof(OnPropertiesChanged), handler);

    public Task<(uint Revision, DBusMenuLayout Layout)> GetLayoutAsync(
        int parentId,
        int recursionDepth,
        string[] propertyNames
    )
    {
        var items = parentId == 0 ? _menu?.Items ?? [] : GetChildren(parentId);
        return Task.FromResult((_revision, BuildLayout(parentId, items, recursionDepth, propertyNames)));
    }

    public Task<DBusMenuProperties[]> GetGroupPropertiesAsync(int[] ids, string[] propertyNames) =>
        Task.FromResult(
            ids.Where(_items.ContainsKey)
                .Select(id => new DBusMenuProperties
                {
                    Id = id,
                    Properties = GetItemProperties(id, _items[id], propertyNames),
                })
                .ToArray()
        );

    public Task<object> GetPropertyAsync(int id, string name)
    {
        if (!_items.TryGetValue(id, out var item))
        {
            throw new ArgumentException($"Unknown tray menu item ID '{id}'.", nameof(id));
        }

        var properties = GetItemProperties(id, item, [name]);
        return properties.TryGetValue(name, out var value)
            ? Task.FromResult(value)
            : throw new ArgumentException($"Unknown tray menu property '{name}'.", nameof(name));
    }

    public async Task EventAsync(int id, string eventId, object data, uint timestamp)
    {
        if (eventId != "clicked" || !_items.TryGetValue(id, out var item) || item is not TrayMenuCommand command)
        {
            return;
        }

        try
        {
            await command.Activated(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _owner.ReportUnhandledException(exception);
        }
    }

    public async Task<int[]> EventGroupAsync(DBusMenuEvent[] events)
    {
        foreach (var menuEvent in events)
        {
            await EventAsync(menuEvent.Id, menuEvent.EventId, menuEvent.Data, menuEvent.Timestamp).ConfigureAwait(false);
        }

        return [];
    }

    public async Task<bool> AboutToShowAsync(int id)
    {
        _icon.RaiseMenuOpening();
        return await RefreshStatesAsync().ConfigureAwait(false);
    }

    public async Task<(int[] UpdatesNeeded, int[] IdErrors)> AboutToShowGroupAsync(int[] ids)
    {
        _icon.RaiseMenuOpening();
        var changed = await RefreshStatesAsync().ConfigureAwait(false);
        return changed ? ([0], []) : ([], []);
    }

    public Task<IDisposable> WatchLayoutUpdatedAsync(Action<(uint Revision, int Parent)> handler) =>
        SignalWatcher.AddAsync(this, nameof(OnLayoutUpdated), handler);

    public Task<IDisposable> WatchItemsPropertiesUpdatedAsync(
        Action<(DBusMenuProperties[] Updated, DBusMenuRemovedProperties[] Removed)> handler
    ) => SignalWatcher.AddAsync(this, nameof(OnItemsPropertiesUpdated), handler);

    private void SetMenu(TrayMenu? menu, bool notify)
    {
        _menu = menu;
        _items = [];
        _states = [];
        var nextId = 1;
        AddItems(menu?.Items ?? [], ref nextId);
        _revision++;
        if (notify)
        {
            OnLayoutUpdated?.Invoke((_revision, 0));
        }
    }

    private void AddItems(IEnumerable<TrayMenuItem> items, ref int nextId)
    {
        foreach (var item in items)
        {
            _items.Add(nextId++, item);
            if (item is TraySubmenu submenu)
            {
                AddItems(submenu.Items, ref nextId);
            }
        }
    }

    private DBusMenuLayout BuildLayout(
        int parentId,
        IReadOnlyList<TrayMenuItem> items,
        int recursionDepth,
        string[] propertyNames
    )
    {
        var children = new List<object>();
        foreach (var item in items)
        {
            var id = _items.First(entry => ReferenceEquals(entry.Value, item)).Key;
            var childItems = item is TraySubmenu submenu && recursionDepth != 0 ? submenu.Items : [];
            children.Add(BuildLayout(id, childItems, recursionDepth < 0 ? -1 : recursionDepth - 1, propertyNames));
        }

        return new DBusMenuLayout
        {
            Id = parentId,
            Properties = parentId == 0 ? new Dictionary<string, object>() : GetItemProperties(parentId, _items[parentId], propertyNames),
            Children = children.ToArray(),
        };
    }

    private IReadOnlyList<TrayMenuItem> GetChildren(int parentId) =>
        _items.TryGetValue(parentId, out var item) && item is TraySubmenu submenu ? submenu.Items : [];

    private IDictionary<string, object> GetItemProperties(int id, TrayMenuItem item, string[] requested)
    {
        Dictionary<string, object> properties = item switch
        {
            TrayMenuSeparator => new() { ["type"] = "separator", ["visible"] = true },
            TrayMenuCommand command => new()
            {
                ["label"] = command.Text,
                ["enabled"] = GetState(id, command).IsEnabled,
                ["visible"] = GetState(id, command).IsVisible,
                ["toggle-type"] = GetState(id, command).IsChecked ? "checkmark" : "",
                ["toggle-state"] = GetState(id, command).IsChecked ? 1 : 0,
            },
            TraySubmenu submenu => new()
            {
                ["label"] = submenu.Text,
                ["enabled"] = submenu.IsEnabled,
                ["visible"] = true,
                ["children-display"] = "submenu",
            },
            _ => new(),
        };

        if (requested.Length == 0)
        {
            return properties;
        }

        return properties.Where(pair => requested.Contains(pair.Key)).ToDictionary();
    }

    private TrayMenuItemState GetState(int id, TrayMenuCommand command) =>
        _states.TryGetValue(id, out var state)
            ? state
            : new TrayMenuItemState(true, command.IsEnabled, command.IsChecked);

    private async Task<bool> RefreshStatesAsync()
    {
        var changed = false;
        foreach (var (id, item) in _items)
        {
            if (item is not TrayMenuCommand { GetState: { } getState } command)
            {
                continue;
            }

            try
            {
                var state = await getState(CancellationToken.None).ConfigureAwait(false);
                if (!_states.TryGetValue(id, out var current) || current != state)
                {
                    _states[id] = state;
                    changed = true;
                }
            }
            catch (Exception exception)
            {
                _owner.ReportUnhandledException(exception);
            }
        }

        if (changed)
        {
            _revision++;
            OnLayoutUpdated?.Invoke((_revision, 0));
        }

        return changed;
    }
}
