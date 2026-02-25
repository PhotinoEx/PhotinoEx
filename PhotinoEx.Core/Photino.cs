using System.Drawing;
using System.Runtime.Versioning;
using PhotinoEx.Core.Enums;
using PhotinoEx.Core.Models;
using Monitor = PhotinoEx.Core.Models.Monitor;
using Size = System.Drawing.Size;

namespace PhotinoEx.Core;

public abstract class Photino
{
    protected Action<string>? _WebMessageReceivedCallback { get; set; }
    protected Action<int, int>? _resizedCallback { get; set; }
    protected Action? _maximizedCallback { get; set; }
    protected Action? _restoredCallback { get; set; }
    protected Action? _minimizedCallback { get; set; }
    protected Action<int, int>? _movedCallback { get; set; }
    protected Func<bool>? _closingCallback { get; set; }
    protected Action? _focusInCallback { get; set; }
    protected Action? _focusOutCallback { get; set; }
    protected List<string> _customSchemeNames { get; set; } = new();
    protected PhotinoInitParams.WebResourceRequestedCallback? _customSchemeCallback { get; set; }

    protected Func<Monitor, int>? _getAllMonitors { get; set; }
    protected string _startUrl { get; set; } = "";
    protected string _startString { get; set; } = "";
    protected string? _temporaryFilesPath { get; set; } = "";
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

    public bool ContextMenuEnabled { get; set; }
    public int MinWidth { get; set; }
    public int MinHeight { get; set; }
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }

    public abstract void ClearBrowserAutoFill();

    public abstract void Close();

    public abstract bool GetTransparentEnabled();

    public abstract bool GetContextMenuEnabled();

    // Tested - linux
    // untested - windows / apple
    public abstract bool GetDevToolsEnabled();

    // Tested - linux
    // untested - windows / apple
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

    // Tested - linux
    // untested - windows / apple
    public abstract bool GetMaximized();

    // Tested - linux
    // untested - windows / apple
    public abstract bool GetMinimized();

    public abstract bool GetResizable();

    public abstract uint GetScreenDpi();

    public abstract Size GetSize();

    public abstract string GetTitle();

    public abstract bool GetTopmost();

    public abstract int GetZoom();

    public abstract bool GetIgnoreCertificateErrorsEnabled();

    [UnsupportedOSPlatform("Linux")]
    public abstract Point GetPosition();

    // Tested - linux / windows
    // untested - apple
    public abstract void NavigateToString(string content);

    // Tested - linux / windows
    // untested - apple
    public abstract void NavigateToUrl(string url);

    public abstract void Restore();

    // Tested - linux / windows
    // untested - apple
    public abstract void SendWebMessage(string message);

    public abstract void SetTransparentEnabled(bool enabled);

    public abstract void SetContextMenuEnabled(bool enabled);

    // Tested - linux
    // untested - windows / apple
    public abstract void SetDevToolsEnabled(bool enabled);

    [UnsupportedOSPlatform("Linux")]
    public abstract void SetPosition(Point newLocation);

    // Tested - linux
    // untested - windows / apple
    public abstract void SetIconFile(string filename);

    // Tested - linux
    // untested - windows / apple
    public abstract void SetFullScreen(bool fullScreen);

    // Tested - linux
    // untested - windows / apple
    public abstract void SetMaximized(bool maximized);

    // Tested - linux
    // untested - windows / apple
    public abstract void SetMinimized(bool minimized);

    public abstract void SetResizable(bool resizable);

    public abstract void SetSize(Size size);

    // Tested - linux / windows
    // untested -  apple
    public abstract void SetTitle(string title);

    // Tested - linux
    // untested -  apple / windows
    [UnsupportedOSPlatform("Linux")]
    public abstract void SetTopmost(bool topmost);

    public abstract void SetZoom(int zoom);

    // Tested - linux
    // untested - windows / apple
    public abstract void ShowNotification(string title, string message);

    // Tested - linux / windows
    // untested - apple
    public abstract void WaitForExit();

    // Tested - linux / windows
    // untested - apple
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

    // Tested - linux / windows
    // untested - apple
    public abstract Task<List<string>> ShowOpenFileAsync(string title, string? path, bool multiSelect, List<FileFilter>? filterPatterns);

    // Tested - linux / windows
    // untested - apple
    public abstract Task<List<string>> ShowOpenFolderAsync(string title, string? path, bool multiSelect);

    // Tested - linux / windows
    // untested - apple
    public abstract Task<string> ShowSaveFileAsync(string title, string? path, List<FileFilter>? filterPatterns, string defaultExtension = "txt", string defaultFileName = "PhotinoExFile");

    // Tested - linux / windows
    // Untested - apple
    public abstract Task<DialogResult> ShowMessageAsync(string title, string text, DialogButtons buttons, DialogIcon icon);

    // Tested - linux / windows
    // untested - apple
    public abstract void Invoke(Action callback);

    public bool InvokeClose()
    {
        return _closingCallback?.Invoke() ?? false;
    }

    public void InvokeFocusIn()
    {
        _focusInCallback?.Invoke();
    }

    public void InvokeFocusOut()
    {
        _focusOutCallback?.Invoke();
    }

    public void InvokeMove(int x, int y)
    {
        _movedCallback?.Invoke(x, y);
    }

    public void InvokeResize(int width, int height)
    {
        _resizedCallback?.Invoke(width, height);
    }

    public void InvokeMaximized()
    {
        _maximizedCallback?.Invoke();
    }

    public void InvokeRestored()
    {
        _restoredCallback?.Invoke();
    }

    public void InvokeMinimized()
    {
        _minimizedCallback?.Invoke();
    }
}
