using Monitor = PhotinoEx.Core.Models.Monitor;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PhotinoEx.Core;

public abstract class Photino
{
    protected Action<string>? _onWebMessageReceived { get; set; }
    protected Action<int, int>? _onResized { get; set; }
    protected Action? _onMaximized { get; set; }
    protected Action? _onRestored { get; set; }
    protected Action? _onMinimized { get; set; }
    protected Action<int, int>? _onMoved { get; set; }
    protected Func<bool>? _onClosing { get; set; }
    protected Action? _onFocusIn { get; set; }
    protected Action? _onFocusOut { get; set; }
    protected Func<Monitor, int>? _getAllMonitors { get; set; }
    protected Func<object>? _onCustomScheme { get; set; } // TODO: this is not correct, but deal with later
    protected List<string> _customSchemeNames { get; set; } = new();

    protected string _startUrl { get; set; } = "";
    protected string _startString { get; set; } = "";
    protected string _temporaryFilesPath { get; set; } = "";
    protected string _windowTitle { get; set; } = "";
    protected string _iconFileName { get; set; } = "";
    protected string _userAgent { get; set; } = "";
    protected string _browserControlInitParameters { get; set; } = "";
    protected string _notificationRegistrationId { get; set; } = "";

    protected bool _transparentEnabled { get; set; }
    protected bool _devToolsEnabled { get; set; }
    protected bool _grantBrowserPermissions { get; set; }
    protected bool _mediaAutoplayEnabled { get; set; }
    protected bool _fileSystemAccessEnabled { get; set; }
    protected bool _webSecurityEnabled { get; set; }
    protected bool _javascriptClipboardAccessEnabled { get; set; }
    protected bool _mediaStreamEnabled { get; set; }
    protected bool _smoothScrollingEnabled { get; set; }
    protected bool _ignoreCertificateErrorsEnabled { get; set; }
    protected bool _notificationsEnabled { get; set; }

    protected int _zoom { get; set; }

    protected Photino? _parent { get; set; }
    protected PhotinoDialog? _dialog { get; set; }

    public bool ContextMenuEnabled { get; set; }
    public int MinWidth { get; set; }
    public int MinHeight { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }

    // public abstract void Show();

    public PhotinoDialog? GetDialog()
    {
        return _dialog;
    }

    // public abstract void Center();

    public abstract void ClearBrowserAutoFill();

    public abstract void Close();

    public abstract bool GetTransparentEnabled();

    public abstract bool GetContextMenuEnabled();

    public abstract bool GetDevToolsEnabled();

    public abstract bool GetFullScreen();

    public abstract bool GetGrantBrowserPermissions();

    public abstract string GetUserAgent();

    public abstract bool GetMediaAutoplayEnabled();

    public abstract bool GetFileSystemAccessEnabled();

    public abstract bool GetWebSecurityEnabled();

    public abstract bool GetJavascriptClipboardAccessEnabled();

    public abstract bool GetMediaStreamEnabled();

    public abstract bool GetSmoothScrollingEnabled();

    public abstract bool GetNotificationsEnabled();

    public abstract string GetIconFileName();

    public abstract bool GetMaximized();

    public abstract bool GetMinimized();

    // public abstract Point GetPosition();

    public abstract bool GetResizable();

    public abstract uint GetScreenDpi();

    public abstract Size GetSize();

    public abstract string GetTitle();

    public abstract bool GetTopmost();

    public abstract int GetZoom();

    public abstract bool GetIgnoreCertificateErrorsEnabled();

    public abstract void NavigateToString(string content);

    public abstract void NavigateToUrl(string url);

    // TODO: this currently doesnt work on linux so maybe look into this
    public abstract void Restore();

    public abstract void SendWebMessage(string message);

    public abstract void SetTransparentEnabled(bool enabled);

    public abstract void SetContextMenuEnabled(bool enabled);

    public abstract void SetDevToolsEnabled(bool enabled);

    public abstract void SetIconFile(string filename);

    public abstract void SetFullScreen(bool fullScreen);

    public abstract void SetMaximized(bool maximized);

    // public abstract void SetMaxSize(Size size);

    // public abstract void SetMinSize(Size size);

    public abstract void SetMinimized(bool minimized);

    // public abstract void SetPosition(Point position);

    public abstract void SetResizable(bool resizable);

    public abstract void SetSize(Size size);

    public abstract void SetTitle(string title);

    public abstract void SetTopmost(bool topmost);

    public abstract void SetZoom(int zoom);

    public abstract void ShowNotification(string title, string message);

    public abstract void WaitForExit();

    public abstract void AddCustomSchemeName(string scheme);

    public abstract List<Monitor> GetAllMonitors();

    public abstract void SetClosingCallback(Func<bool> callback);

    public abstract void SetFocusInCallback(Action callback);

    public abstract void SetFocusOutCallback(Action callback);

    public abstract void SetMovedCallback(Action<int, int> callback);

    public abstract void SetResizedCallback(Action<int, int> callback);

    public abstract void SetMaximizedCallback(Action callback);

    public abstract void SetRestoredCallback(Action callback);

    public abstract void SetMinimizedCallback(Action callback);

    public abstract void Invoke(Action callback);

    public bool InvokeClose()
    {
        return _onClosing?.Invoke() ?? false;
    }

    public void InvokeFocusIn()
    {
        _onFocusIn?.Invoke();
    }

    public void InvokeFocusOut()
    {
        _onFocusOut?.Invoke();
    }

    public void InvokeMove(int x, int y)
    {
        _onMoved?.Invoke(x, y);
    }

    public void InvokeResize(int width, int height)
    {
        _onResized?.Invoke(width, height);
    }

    public void InvokeMaximized()
    {
        _onMaximized?.Invoke();
    }

    public void InvokeRestored()
    {
        _onRestored?.Invoke();
    }

    public void InvokeMinimized()
    {
        _onMinimized?.Invoke();
    }
}
