using PhotinoEx.Core.Platform;

namespace PhotinoEx.Core.Models;

public class PhotinoInitParams
{
    public string StartString { get; set; } = "";
    public string StartUrl { get; set; } = "";
    public string Title { get; set; } = "";
    public string WindowIconFile { get; set; } = "";
    public string? TemporaryFilesPath { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public string BrowserControlInitParameters { get; set; } = "";
    public string NotificationRegistrationId { get; set; } = "";

    public Photino? ParentInstance { get; set; }

    public Func<bool>? ClosingHandler { get; set; }
    public Action? FocusInHandler { get; set; }
    public Action? FocusOutHandler { get; set; }
    public Action<int, int>? ResizedHandler { get; set; }
    public Action? MaximizedHandler { get; set; }
    public Action? RestoredHandler { get; set; }
    public Action? MinimizedHandler { get; set; }
    public Action<int, int>? MovedHandler { get; set; }
    public Action<string>? WebMessageRecievedHandler { get; set; }
    public List<string>? CustomSchemeNames;
    public WebResourceRequestedCallback? CustomSchemeHandler { get; set; }

    public delegate MemoryStream WebResourceRequestedCallback(string url, out string outContentType);

    public Func<Monitor, int>? GetAllMonitors { get; set; }

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
    public bool UseOsDefaultLocation;

    public bool Chromeless;
    public bool Transparent;
    public bool ContextMenuEnabled;
    public bool DevToolsEnabled;
    public bool FullScreen;
    public bool Maximized;
    public bool Minimized;
    public bool Resizable;
    public bool Topmost;
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

    public List<string> GetParamErrors()
    {
        var response = new List<string>();
        var startUrl = StartUrl;
        var startString = StartString;

        if (string.IsNullOrWhiteSpace(startUrl) && string.IsNullOrWhiteSpace(startString))
        {
            response.Add("An initial URL or HTML string must be supplied in StartUrl or StartString for the browser control to naviage to.");
        }

        if (Maximized && Minimized)
        {
            response.Add("Window cannot be both maximized and minimized on startup.");
        }

        if (FullScreen && (Maximized || Minimized))
        {
            response.Add("FullScreen cannot be combined with Maximized or Minimized");
        }

        Size = 0;

        return response;
    }
}
