using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using PhotinoEx.Core.Models;

namespace PhotinoEx.Core.Platform.Windows.Tray;

[SupportedOSPlatform("windows")]
internal sealed class WinPhotinoExTrayIcon : IPhotinoExTrayIcon
{
    private const uint NIM_ADD = 0;
    private const uint NIM_MODIFY = 1;
    private const uint NIM_DELETE = 2;
    private const uint NIF_MESSAGE = 1;
    private const uint NIF_ICON = 2;
    private const uint NIF_TIP = 4;
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x10;
    private const uint LR_DEFAULTSIZE = 0x40;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_MBUTTONUP = 0x0208;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint MF_STRING = 0;
    private const uint MF_SEPARATOR = 0x0800;
    private const uint MF_POPUP = 0x0010;
    private const uint MF_GRAYED = 0x0001;
    private const uint MF_CHECKED = 0x0008;
    private readonly WinPhotinoExTray _owner;
    private readonly WinTrayNative _native;
    private readonly uint _nativeId;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SynchronizationContext? _synchronizationContext;
    private string _iconPath;
    private string? _toolTip;
    private TrayMenu? _menu;
    private IntPtr _iconHandle;
    private bool _disposed;

    internal WinPhotinoExTrayIcon(WinPhotinoExTray owner, WinTrayNative native, TrayIconOptions options, uint nativeId)
    {
        _owner = owner;
        _native = native;
        _nativeId = nativeId;
        Id = options.Id;
        _iconPath = options.IconPath;
        _toolTip = options.ToolTip;
        _menu = options.Menu;
        IsVisible = options.IsVisible;
        _synchronizationContext = SynchronizationContext.Current;
    }

    public string Id { get; }
    public bool IsVisible { get; private set; }
    public event EventHandler<TrayIconEventArgs>? Clicked;
    public event EventHandler? MenuOpening;

    internal Task RegisterAsync() => _native.InvokeAsync(() =>
    {
        _iconHandle = LoadIcon(_iconPath);
        _native.Add(_nativeId, this);
        if (IsVisible && !Shell_NotifyIconW(NIM_ADD, CreateData()))
        {
            throw new TrayIconException($"Shell_NotifyIcon failed with error {Marshal.GetLastWin32Error()}.");
        }
    });

    public async Task SetIconAsync(string iconPath)
    {
        if (!File.Exists(iconPath))
        {
            throw new FileNotFoundException("The tray icon file does not exist.", iconPath);
        }
        await MutateAsync(() =>
        {
            var replacement = LoadIcon(iconPath);
            var previous = _iconHandle;
            _iconPath = iconPath;
            _iconHandle = replacement;
            if (IsVisible) Shell_NotifyIconW(NIM_MODIFY, CreateData());
            if (previous != IntPtr.Zero) DestroyIcon(previous);
        }).ConfigureAwait(false);
    }

    public Task SetToolTipAsync(string? toolTip) => MutateAsync(() =>
    {
        _toolTip = toolTip;
        if (IsVisible) Shell_NotifyIconW(NIM_MODIFY, CreateData());
    });

    public Task SetVisibleAsync(bool visible) => MutateAsync(() =>
    {
        if (visible == IsVisible) return;
        IsVisible = visible;
        Shell_NotifyIconW(visible ? NIM_ADD : NIM_DELETE, CreateData());
    });

    public Task SetMenuAsync(TrayMenu? menu)
    {
        menu?.Validate();
        return MutateAsync(() => _menu = menu);
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _disposed = true;
            await _native.InvokeAsync(() =>
            {
                if (IsVisible) Shell_NotifyIconW(NIM_DELETE, CreateData());
                _native.Remove(_nativeId);
                if (_iconHandle != IntPtr.Zero) DestroyIcon(_iconHandle);
                _iconHandle = IntPtr.Zero;
            }).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal void HandleNativeEvent(uint message, IntPtr window)
    {
        switch (message)
        {
            case WM_LBUTTONUP:
                RaiseClicked(TrayMouseButton.Primary, 1);
                break;
            case WM_LBUTTONDBLCLK:
                RaiseClicked(TrayMouseButton.Primary, 2);
                break;
            case WM_MBUTTONUP:
                RaiseClicked(TrayMouseButton.Middle, 1);
                break;
            case WM_RBUTTONUP:
                RaiseClicked(TrayMouseButton.Secondary, 1);
                ShowMenu(window);
                break;
        }
    }

    private void ShowMenu(IntPtr window)
    {
        Dispatch(() => MenuOpening?.Invoke(this, EventArgs.Empty));
        if (_menu is null || _menu.Items.Count == 0) return;

        var commands = new Dictionary<uint, TrayMenuCommand>();
        uint nextCommand = 1;
        var nativeMenu = BuildMenu(_menu.Items, commands, ref nextCommand);
        try
        {
            GetCursorPos(out var point);
            SetForegroundWindow(window);
            var selected = TrackPopupMenuEx(nativeMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON, point.X, point.Y, window, IntPtr.Zero);
            if (selected != 0 && commands.TryGetValue(selected, out var command))
            {
                _ = ExecuteCommandAsync(command);
            }
        }
        finally
        {
            DestroyMenu(nativeMenu);
        }
    }

    private IntPtr BuildMenu(IReadOnlyList<TrayMenuItem> items, Dictionary<uint, TrayMenuCommand> commands, ref uint nextCommand)
    {
        var menu = CreatePopupMenu();
        foreach (var item in items)
        {
            switch (item)
            {
                case TrayMenuSeparator:
                    AppendMenuW(menu, MF_SEPARATOR, UIntPtr.Zero, null);
                    break;
                case TrayMenuCommand command:
                    var state = GetCommandState(command);
                    if (!state.IsVisible)
                    {
                        break;
                    }
                    var commandId = nextCommand++;
                    commands[commandId] = command;
                    var flags = MF_STRING | (state.IsEnabled ? 0 : MF_GRAYED) | (state.IsChecked ? MF_CHECKED : 0);
                    AppendMenuW(menu, flags, (UIntPtr)commandId, command.Text);
                    break;
                case TraySubmenu submenu:
                    var child = BuildMenu(submenu.Items, commands, ref nextCommand);
                    AppendMenuW(menu, MF_POPUP | (submenu.IsEnabled ? 0 : MF_GRAYED), (UIntPtr)child, submenu.Text);
                    break;
            }
        }
        return menu;
    }

    private TrayMenuItemState GetCommandState(TrayMenuCommand command)
    {
        if (command.GetState is null)
        {
            return new TrayMenuItemState(true, command.IsEnabled, command.IsChecked);
        }

        try
        {
            return command.GetState(CancellationToken.None).AsTask().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            _owner.ReportUnhandledException(exception);
            return new TrayMenuItemState(false, false, false);
        }
    }

    private async Task ExecuteCommandAsync(TrayMenuCommand command)
    {
        try
        {
            await command.Activated(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _owner.ReportUnhandledException(exception);
        }
    }

    private async Task MutateAsync(Action action)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await _native.InvokeAsync(action).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void RaiseClicked(TrayMouseButton button, int count) => Dispatch(() =>
        Clicked?.Invoke(this, new TrayIconEventArgs { Button = button, ClickCount = count })
    );

    private void Dispatch(Action action)
    {
        void Invoke()
        {
            try { action(); }
            catch (Exception exception) { _owner.ReportUnhandledException(exception); }
        }
        if (_synchronizationContext is null) Invoke();
        else _synchronizationContext.Post(_ => Invoke(), null);
    }

    private NotifyIconData CreateData() => new()
    {
        CbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
        HWnd = _native.WindowHandle,
        UId = _nativeId,
        UFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
        UCallbackMessage = WinTrayNative.CallbackMessage,
        HIcon = _iconHandle,
        SzTip = (_toolTip ?? Id)[..Math.Min((_toolTip ?? Id).Length, 127)],
    };

    private static IntPtr LoadIcon(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".ico", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("Windows tray icons currently require an .ico file.");
        }
        var icon = LoadImageW(IntPtr.Zero, Path.GetFullPath(path), IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
        return icon != IntPtr.Zero ? icon : throw new TrayIconException($"Could not load tray icon '{path}'.");
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint CbSize;
        public IntPtr HWnd;
        public uint UId;
        public uint UFlags;
        public uint UCallbackMessage;
        public IntPtr HIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string SzTip;
        public uint DwState;
        public uint DwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string SzInfo;
        public uint UTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string SzInfoTitle;
        public uint DwInfoFlags;
        public Guid GuidItem;
        public IntPtr HBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Shell_NotifyIconW(uint message, ref NotifyIconData data);
    private static bool Shell_NotifyIconW(uint message, NotifyIconData data) => Shell_NotifyIconW(message, ref data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr LoadImageW(IntPtr instance, string name, uint type, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenuW(IntPtr menu, uint flags, UIntPtr id, string? text);
    [DllImport("user32.dll")] private static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")] private static extern uint TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr window, IntPtr parameters);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
}
