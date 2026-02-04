using Monitor = PhotinoEx.Core.Models.Monitor;

namespace PhotinoEx.Core;

public class PhotinoInitParams
{
    public string? StartString { get; set; }
    public string? StartUrl { get; set; }
    public string? Title { get; set; }
    public string? WindowIconFile { get; set; }
    public string? TemporaryFilesPath { get; set; }
    public string? UserAgent { get; set; }
    public string? BrowserControlInitParameters { get; set; }
    public string? NotificationRegistrationId { get; set; }

    public Photino? ParentInstance { get; set; }

    public Action? OnAction { get; set; }
    public Action<string>? OnWebMessageReceived { get; set; }
    public Action<int, int>? OnResized { get; set; }
    public Action? OnMaximized { get; set; }
    public Action? OnRestored { get; set; }
    public Action? OnMinimized { get; set; }
    public Action<int, int>? OnMoved { get; set; }
    public Func<bool>? OnClosing { get; set; }
    public Action? OnFocusIn { get; set; }
    public Action? OnFocusOut { get; set; }
    public Func<Monitor, int>? GetAllMonitors { get; set; }

    public delegate IntPtr WebResourceRequestedCallback(string url, out int outNumBytes, out string outContentType);

    public List<string>? CustomSchemeNames;

    public int Left;
    public int Top;
    public int Width;
    public int Height;
    public int Zoom;
    public int MinWidth;
    public int MinHeight;
    public int MaxWidth;
    public int MaxHeight;

    public bool CenterOnInitialize;
    public bool Chromeless;
    public bool Transparent;
    public bool ContextMenuEnabled;
    public bool DevToolsEnabled;
    public bool FullScreen;
    public bool Maximized;
    public bool Minimized;
    public bool Resizable;
    public bool Topmost;
    public bool UseOsDefaultLocation;
    public bool UseOsDefaultSize;
    public bool GrantBrowserPermissions;
    public bool MediaAutoplayEnabled;
    public bool FileSystemAccessEnabled;
    public bool WebSecurityEnabled;
    public bool JavascriptClipboardAccessEnabled;
    public bool MediaStreamEnabled;
    public bool SmoothScrollingEnabled;
    public bool IgnoreCertificateErrorsEnabled;
    public bool NotificationsEnabled;

    public int Size;
}
