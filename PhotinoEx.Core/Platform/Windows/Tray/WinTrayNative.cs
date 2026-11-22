using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PhotinoEx.Core.Platform.Windows.Tray;

[SupportedOSPlatform("windows")]
internal sealed class WinTrayNative : IDisposable
{
    internal const uint CallbackMessage = WM_APP + 1;
    private const uint InvokeMessage = WM_APP + 2;
    private const uint WM_APP = 0x8000;
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_CLOSE = 0x0010;
    private readonly ConcurrentQueue<Action> _work = [];
    private readonly Dictionary<uint, WinPhotinoExTrayIcon> _icons = [];
    private readonly TaskCompletionSource<IntPtr> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread _thread;
    private WndProc? _wndProc;
    private IntPtr _window;

    internal WinTrayNative()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "PhotinoEx tray" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    internal IntPtr WindowHandle => _ready.Task.GetAwaiter().GetResult();

    internal async Task InvokeAsync(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _work.Enqueue(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        PostMessageW(await _ready.Task.ConfigureAwait(false), InvokeMessage, IntPtr.Zero, IntPtr.Zero);
        await completion.Task.ConfigureAwait(false);
    }

    internal void Add(uint nativeId, WinPhotinoExTrayIcon icon) => _icons.Add(nativeId, icon);
    internal void Remove(uint nativeId) => _icons.Remove(nativeId);

    public void Dispose()
    {
        if (_window != IntPtr.Zero)
        {
            PostMessageW(_window, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(TimeSpan.FromSeconds(2));
            _window = IntPtr.Zero;
        }
    }

    private void Run()
    {
        _wndProc = WindowProc;
        var className = $"PhotinoExTray-{Environment.ProcessId}-{Guid.NewGuid():N}";
        var wndClass = new WndClass
        {
            LpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            HInstance = GetModuleHandleW(null),
            LpszClassName = className,
        };
        RegisterClassW(ref wndClass);
        _window = CreateWindowExW(0, className, className, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, wndClass.HInstance, IntPtr.Zero);
        if (_window == IntPtr.Zero)
        {
            _ready.SetException(new TrayIconException("Could not create the Windows tray message window."));
            return;
        }

        _ready.SetResult(_window);
        while (GetMessageW(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref message);
            DispatchMessageW(ref message);
        }
    }

    private IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == InvokeMessage)
        {
            while (_work.TryDequeue(out var action))
            {
                action();
            }
            return IntPtr.Zero;
        }

        if (message == CallbackMessage)
        {
            var id = unchecked((uint)wParam.ToInt64());
            if (_icons.TryGetValue(id, out var icon))
            {
                icon.HandleNativeEvent(unchecked((uint)lParam.ToInt64()) & 0xffff, hwnd);
            }
            return IntPtr.Zero;
        }

        if (message == WM_DESTROY)
        {
            PostQuitMessage(0);
            return IntPtr.Zero;
        }

        return DefWindowProcW(hwnd, message, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClass
    {
        public uint Style;
        public IntPtr LpfnWndProc;
        public int ClsExtra;
        public int WndExtra;
        public IntPtr HInstance;
        public IntPtr HIcon;
        public IntPtr HCursor;
        public IntPtr HbrBackground;
        public string? LpszMenuName;
        public string LpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public IntPtr Hwnd;
        public uint Value;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X;
        public int Y;
        public uint Private;
    }

    private delegate IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassW(ref WndClass wndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessageW(out Message message, IntPtr hwnd, uint min, uint max);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Message message);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessageW(ref Message message);

    [DllImport("user32.dll")]
    private static extern bool PostMessageW(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? moduleName);
}
