using Monitor = PhotinoEx.Core.Models.Monitor;

namespace PhotinoEx.Core;

public abstract class Photino
{
    private Action<string>? _onWebMessageReceived { get; set; }
    private Action<int, int>? _onResized { get; set; }
    private Action? _onMaximized { get; set; }
    private Action? _onRestored { get; set; }
    private Action? _onMinimized { get; set; }
    private Action<int, int>? _onMoved { get; set; }
    private Func<bool>? _onClosing { get; set; }
    private Action? _onFocusIn { get; set; }
    private Action? _onFocusOut { get; set; }
    private Func<Monitor, int>? _getAllMonitors { get; set; }

    private string? _startUrl { get; set; }
    private string? _startString { get; set; }
    private string? _temporaryFilesPath { get; set; }
    private string? _windowTitle { get; set; }
    private string? _iconFileName { get; set; }
    private string? _userAgent { get; set; }
    private string? _browserControlInitParameters { get; set; }
    private string? _notificationRegistrationId { get; set; }

    private bool _transparentEnabled { get; set; }
    private bool _devToolsEnabled { get; set; }
    private bool _grantBrowserPermissions { get; set; }
    private bool _mediaAutoplayEnabled { get; set; }
    private bool _fileSystemAccessEnabled { get; set; }
    private bool _webSecurityEnabled { get; set; }
    private bool _javascriptClipboardAccessEnabled { get; set; }
    private bool _mediaStreamEnabled { get; set; }
    private bool _smoothScrollingEnabled { get; set; }
    private bool _ignoreCertificateErrorsEnabled { get; set; }
    private bool _notificationsEnabled { get; set; }

    private int _zoom { get; set; }

    private Photino? _parent { get; set; }
    private PhotinoDialog? _dialog { get; set; }

    public bool ContextMenuEnabled { get; set; }
    public int MinWidth { get; set; }
    public int MinHeight { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }

    protected abstract void Show(bool isAlreadyShown);

    public PhotinoDialog? GetDialog()
    {
        return _dialog;
    }

    public abstract void Center();

    public abstract void ClearBrowserAutoFill();

    public abstract void GetTransparentEnabled(bool enabled);

    public abstract void GetContextMenuEnabled(bool enabled);

    public abstract void GetDevToolsEnabled(bool enabled);

    public abstract void GetFullScreen(bool fullScreen);

    public abstract void GetGrantBrowserPermissions(bool grant);

    public abstract string GetUserAgent();

    public abstract void GetMediaAutoplayEnabled(bool enabled);

    public abstract void GetFileSystemAccessEnabled(bool enabled);

    public abstract void GetWebSecurityEnabled(bool enabled);

    public abstract void GetJavascriptClipboardAccessEnabled(bool enabled);

    public abstract void GetMediaStreamEnabled(bool enabled);

    public abstract void GetSmoothScrollingEnabled(bool enabled);

    public abstract string GetIconFileName();

    public abstract void GetMaximized(bool isMaximized);

    public abstract void GetMinimized(bool isMinimized);

    public abstract void GetPosition(int x, int y);

    public abstract void GetResizable(bool resizable);

    public abstract int GetScreenDpi();

    public abstract void GetSize(int width, int height);

    public abstract string GetTitle();

    public abstract void GetTopmost(bool topmost);

    public abstract void GetZoom(int zoom);

    public abstract void GetIgnoreCertificateErrorsEnabled(bool enabled);

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

    public abstract void SetMaxSize(int width, int height);

    public abstract void SetMinimized(bool minimized);

    public abstract void SetMinSize(int width, int height);

    public abstract void SetPosition(int x, int y);

    public abstract void SetResizable(bool resizable);

    public abstract void SetSize(int width, int height);

    public abstract void SetTitle(string title);

    public abstract void SetTopmost(bool topmost);

    public abstract void SetZoom(int zoom);

    public abstract void ShowNotification(string title, string message);

    public abstract void WaitForExit();

    public abstract void AddCustomSchemeName(string scheme);

    public abstract void GetAllMonitors(Func<Monitor, int> callback);

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
