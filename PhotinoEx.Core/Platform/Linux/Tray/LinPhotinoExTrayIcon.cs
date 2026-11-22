using PhotinoEx.Core.Models;
using Tmds.DBus;

namespace PhotinoEx.Core.Platform.Linux.Tray;

[DBusInterface(
    "org.kde.StatusNotifierItem",
    GetPropertyMethod = nameof(IStatusNotifierItem.GetPropertyAsync),
    GetAllPropertiesMethod = nameof(IStatusNotifierItem.GetAllPropertiesAsync),
    WatchPropertiesMethod = nameof(IStatusNotifierItem.WatchPropertiesAsync)
)]
public interface IStatusNotifierItem : IDBusObject
{
    Task<object> GetPropertyAsync(string property);
    Task<IDictionary<string, object>> GetAllPropertiesAsync();
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    Task ActivateAsync(int x, int y);
    Task SecondaryActivateAsync(int x, int y);
    Task ContextMenuAsync(int x, int y);
    Task ScrollAsync(int delta, string orientation);
}

[DBusInterface("org.kde.StatusNotifierWatcher")]
public interface IStatusNotifierWatcher : IDBusObject
{
    Task RegisterStatusNotifierItemAsync(string service);
}

internal sealed class LinPhotinoExTrayIcon : IPhotinoExTrayIcon, IStatusNotifierItem
{
    private static readonly ObjectPath StatusNotifierPath = new("/StatusNotifierItem");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly LinPhotinoExTray _owner;
    private readonly string _busName;
    private readonly SynchronizationContext? _synchronizationContext;
    private Connection? _connection;
    private LinPhotinoExTrayMenu? _nativeMenu;
    private string _iconPath;
    private string? _toolTip;
    private TrayMenu? _menu;
    private bool _disposed;

    internal LinPhotinoExTrayIcon(LinPhotinoExTray owner, TrayIconOptions options, int instance)
    {
        _owner = owner;
        Id = options.Id;
        _iconPath = options.IconPath;
        _toolTip = options.ToolTip;
        _menu = options.Menu;
        IsVisible = options.IsVisible;
        _busName = $"org.kde.StatusNotifierItem-{Environment.ProcessId}-{instance}";
        _synchronizationContext = SynchronizationContext.Current;
    }

    public string Id { get; }
    public bool IsVisible { get; private set; }
    public ObjectPath ObjectPath => StatusNotifierPath;

    public event EventHandler<TrayIconEventArgs>? Clicked;
    public event EventHandler? MenuOpening;
    public event Action<PropertyChanges>? OnPropertiesChanged;

    internal async Task RegisterAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _connection = new Connection(Address.Session);
        await _connection.ConnectAsync().ConfigureAwait(false);
        await _connection.RegisterServiceAsync(_busName).ConfigureAwait(false);

        _nativeMenu = new LinPhotinoExTrayMenu(_owner, this, _menu);
        await _connection.RegisterObjectsAsync([this, _nativeMenu]).ConfigureAwait(false);

        var watcher = _connection.CreateProxy<IStatusNotifierWatcher>(
            "org.kde.StatusNotifierWatcher",
            new ObjectPath("/StatusNotifierWatcher")
        );
        await watcher.RegisterStatusNotifierItemAsync(_busName).ConfigureAwait(false);
    }

    public async Task SetIconAsync(string iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            throw new ArgumentException("An icon path is required.", nameof(iconPath));
        }

        if (!File.Exists(iconPath))
        {
            throw new FileNotFoundException("The tray icon file does not exist.", iconPath);
        }

        await MutateAsync(() =>
        {
            _iconPath = iconPath;
            RaisePropertiesChanged(nameof(StatusNotifierProperties.IconName), Path.GetFullPath(iconPath));
        }).ConfigureAwait(false);
    }

    public Task SetToolTipAsync(string? toolTip) => MutateAsync(() =>
    {
        _toolTip = toolTip;
        RaisePropertiesChanged(nameof(StatusNotifierProperties.ToolTip), CreateToolTip());
    });

    public Task SetVisibleAsync(bool visible) => MutateAsync(() =>
    {
        IsVisible = visible;
        RaisePropertiesChanged(nameof(StatusNotifierProperties.Status), visible ? "Active" : "Passive");
    });

    public Task SetMenuAsync(TrayMenu? menu)
    {
        menu?.Validate();
        return MutateAsync(() =>
        {
            _menu = menu;
            _nativeMenu!.SetMenu(menu);
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_connection is not null)
            {
                _connection.UnregisterObjects([this, _nativeMenu!]);
                await _connection.UnregisterServiceAsync(_busName).ConfigureAwait(false);
                _connection.Dispose();
                _connection = null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<object> GetPropertyAsync(string property)
    {
        IDictionary<string, object> properties = GetProperties();
        return properties.TryGetValue(property, out var value)
            ? Task.FromResult(value)
            : throw new ArgumentException($"Unknown StatusNotifierItem property '{property}'.", nameof(property));
    }

    public Task<IDictionary<string, object>> GetAllPropertiesAsync() => Task.FromResult(GetProperties());

    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler) =>
        SignalWatcher.AddAsync(this, nameof(OnPropertiesChanged), handler);

    public Task ActivateAsync(int x, int y)
    {
        RaiseClicked(TrayMouseButton.Primary);
        return Task.CompletedTask;
    }

    public Task SecondaryActivateAsync(int x, int y)
    {
        RaiseClicked(TrayMouseButton.Secondary);
        RaiseMenuOpening();
        return Task.CompletedTask;
    }

    public Task ContextMenuAsync(int x, int y)
    {
        RaiseClicked(TrayMouseButton.Secondary);
        RaiseMenuOpening();
        return Task.CompletedTask;
    }

    public Task ScrollAsync(int delta, string orientation) => Task.CompletedTask;

    internal void RaiseMenuOpening() => Dispatch(() => MenuOpening?.Invoke(this, EventArgs.Empty));

    private IDictionary<string, object> GetProperties() => new Dictionary<string, object>
    {
        [nameof(StatusNotifierProperties.Category)] = "ApplicationStatus",
        [nameof(StatusNotifierProperties.Id)] = Id,
        [nameof(StatusNotifierProperties.Title)] = _toolTip ?? Id,
        [nameof(StatusNotifierProperties.Status)] = IsVisible ? "Active" : "Passive",
        [nameof(StatusNotifierProperties.WindowId)] = (uint)0,
        [nameof(StatusNotifierProperties.IconName)] = Path.GetFullPath(_iconPath),
        [nameof(StatusNotifierProperties.IconThemePath)] = Path.GetDirectoryName(Path.GetFullPath(_iconPath)) ?? "",
        [nameof(StatusNotifierProperties.OverlayIconName)] = "",
        [nameof(StatusNotifierProperties.AttentionIconName)] = "",
        [nameof(StatusNotifierProperties.AttentionMovieName)] = "",
        [nameof(StatusNotifierProperties.ToolTip)] = CreateToolTip(),
        [nameof(StatusNotifierProperties.ItemIsMenu)] = false,
        [nameof(StatusNotifierProperties.Menu)] = LinPhotinoExTrayMenu.MenuPath,
    };

    private (string IconName, (int Width, int Height, byte[] Data)[] IconPixmap, string Title, string Description) CreateToolTip() =>
        (Path.GetFullPath(_iconPath), [], _toolTip ?? Id, _toolTip ?? "");

    private async Task MutateAsync(Action action)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            action();
        }
        finally
        {
            _gate.Release();
        }
    }

    private void RaisePropertiesChanged(string property, object value) =>
        OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty(property, value));

    private void RaiseClicked(TrayMouseButton button) => Dispatch(() =>
        Clicked?.Invoke(this, new TrayIconEventArgs { Button = button, ClickCount = 1 })
    );

    private void Dispatch(Action action)
    {
        if (_synchronizationContext is null || SynchronizationContext.Current == _synchronizationContext)
        {
            InvokeSafely(action);
        }
        else
        {
            _synchronizationContext.Post(_ => InvokeSafely(action), null);
        }
    }

    private void InvokeSafely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            _owner.ReportUnhandledException(exception);
        }
    }

    private static class StatusNotifierProperties
    {
        public const string Category = nameof(Category);
        public const string Id = nameof(Id);
        public const string Title = nameof(Title);
        public const string Status = nameof(Status);
        public const string WindowId = nameof(WindowId);
        public const string IconName = nameof(IconName);
        public const string IconThemePath = nameof(IconThemePath);
        public const string OverlayIconName = nameof(OverlayIconName);
        public const string AttentionIconName = nameof(AttentionIconName);
        public const string AttentionMovieName = nameof(AttentionMovieName);
        public const string ToolTip = nameof(ToolTip);
        public const string ItemIsMenu = nameof(ItemIsMenu);
        public const string Menu = nameof(Menu);
    }
}
