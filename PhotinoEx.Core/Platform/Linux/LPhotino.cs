using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Gdk.Internal;
using Gio;
using Gtk;
using WebKit;
using Action = System.Action;
using Application = Gtk.Application;
using Monitor = PhotinoEx.Core.Models.Monitor;
using Window = Gtk.Window;
using Size = System.Drawing.Size;
using Settings = WebKit.Settings;

namespace PhotinoEx.Core.Platform.Linux;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class LPhotino : Photino
{
    public LPhotino(PhotinoInitParams initParams)
    {
        InitParams = initParams;

        _windowTitle = string.IsNullOrEmpty(InitParams.Title) ? "Set a title" : InitParams.Title;
        _startUrl = InitParams.StartUrl;
        _startString = InitParams.StartString;
        _temporaryFilesPath = InitParams.TemporaryFilesPath;
        _userAgent = InitParams.UserAgent;
        _browserControlInitParameters = InitParams.BrowserControlInitParameters;

        _transparentEnabled = InitParams.Transparent;
        _devToolsEnabled = InitParams.DevToolsEnabled;
        _grantBrowserPermissions = InitParams.GrantBrowserPermissions;
        _mediaAutoplayEnabled = InitParams.MediaAutoplayEnabled;
        _fileSystemAccessEnabled = InitParams.FileSystemAccessEnabled;
        _webSecurityEnabled = InitParams.WebSecurityEnabled;
        _javascriptClipboardAccessEnabled = InitParams.JavascriptClipboardAccessEnabled;
        _mediaStreamEnabled = InitParams.MediaStreamEnabled;
        _smoothScrollingEnabled = InitParams.SmoothScrollingEnabled;
        _ignoreCertificateErrorsEnabled = InitParams.IgnoreCertificateErrorsEnabled;
        _isFullScreen = InitParams.FullScreen;

        ContextMenuEnabled = InitParams.ContextMenuEnabled;

        _zoom = InitParams.Zoom;
        MinWidth = InitParams.MinWidth;
        MinWidth = InitParams.MinHeight;
        MaxWidth = InitParams.MaxWidth;
        MaxHeight = InitParams.MaxHeight;

        _onWebMessageReceived = InitParams.OnWebMessageReceived;
        _onResized = InitParams.OnResized;
        _onMoved = InitParams.OnMoved;
        _onClosing = InitParams.OnClosing;
        _onFocusIn = InitParams.OnFocusIn;
        _onFocusOut = InitParams.OnFocusOut;
        _onMaximized = InitParams.OnMaximized;
        _onMinimized = InitParams.OnMinimized;
        _onRestored = InitParams.OnRestored;
        _onCustomScheme = InitParams.OnCustomScheme; // TODO: this is not correct, but deal with later


        if (InitParams.CustomSchemeNames?.Count > 16)
        {
            throw new ApplicationException("too many custom schemes, 16 max");
        }

        foreach (var schemes in InitParams.CustomSchemeNames)
        {
            _customSchemeNames.Add(schemes);
        }

        _parent = InitParams.ParentInstance;
        App = Application.New($"com.photinoex.App", ApplicationFlags.FlagsNone);
        WebKit.Module.Initialize();
        App.OnActivate += App_OnActivate;
        _dialog = new LPhotinoDialog();

        SetTitle(_windowTitle);

        if (InitParams.Chromeless)
        {
            Window.SetDecorated(false);
        }

        if (!string.IsNullOrEmpty(InitParams.WindowIconFile))
        {
            SetIconFile(InitParams.WindowIconFile);
        }

        if (InitParams.Minimized)
        {
            SetMinimized(true);
        }

        if (InitParams.Maximized)
        {
            SetMaximized(true);
        }

        if (!InitParams.Resizable)
        {
            SetResizable(false);
        }

        if (InitParams.Topmost)
        {
            SetTopmost(true);
        }

        // Window.FocusInEvent += OnFocusInEvent;
        // Window.FocusOutEvent += OnFocusOutEvent;

        // These must be called after the webview control is initialized.
        // g_signal_connect(G_OBJECT(_webview), "context-menu",
        //     G_CALLBACK(on_webview_context_menu),
        //     this);
        //
        // g_signal_connect(G_OBJECT(_webview), "permission-request",
        //     G_CALLBACK(on_permission_request),
        //     this);

        if (InitParams.Transparent)
        {
            SetTransparentEnabled(true);
        }

        if (_zoom != 100)
        {
            SetZoom(_zoom);
        }
    }

    private void App_OnActivate(Gio.Application sender, EventArgs args)
    {
        _webView = WebView.New();
        _webView.HeightRequest = 500;
        _webView.WidthRequest = 500;
        AddCustomSchemeHandlers();
        SetWebkitSettings();
        var contentManager = UserContentManager.New();

        string scriptSource = @"
            window.__receiveMessageCallbacks = [];
            window.__dispatchMessageCallback = function(message) {
                window.__receiveMessageCallbacks.forEach(function(callback) {
                    callback(message);
                });
            };
            window.external = {
                sendMessage: function(message) {
                    window.webkit.messageHandlers.Photinointerop.postMessage(message);
                },
                receiveMessage: function(callback) {
                    window.__receiveMessageCallbacks.push(callback);
                }
            };";

        var script = UserScript.New(
            scriptSource,
            UserContentInjectedFrames.AllFrames,
            UserScriptInjectionTime.Start,
            null,
            null
        );

        contentManager.AddScript(script);

        // Register script message handler
        contentManager.RegisterScriptMessageHandler("Photinointerop", null);

        // Connect to script message received event
        contentManager.OnScriptMessageReceived += HandleWebMessage;

        // Navigate to initial content
        if (!string.IsNullOrEmpty(_startUrl))
        {
            NavigateToUrl(_startUrl);
        }
        else if (!string.IsNullOrEmpty(_startString))
        {
            NavigateToString(_startString);
        }
        else
        {
            Environment.Exit(0);
        }

        Window = ApplicationWindow.New((Application) sender);
        Window!.SetChild(_webView);

        if (InitParams.FullScreen)
        {
            Window.Fullscreen();
        }
        else
        {
            if (InitParams.Width > InitParams.MaxWidth)
            {
                InitParams.Width = InitParams.MaxWidth;
            }

            if (InitParams.Height > InitParams.MaxHeight)
            {
                InitParams.Height = InitParams.MaxHeight;
            }

            if (InitParams.Width < InitParams.MinWidth)
            {
                InitParams.Width = InitParams.MinWidth;
            }

            if (InitParams.Height < InitParams.MinWidth)
            {
                InitParams.Height = InitParams.MinWidth;
            }

            if (InitParams.UseOsDefaultSize)
            {
                Window.SetDefaultSize(-1, -1);
            }
            else
            {
                Window.SetDefaultSize(InitParams.Width, InitParams.Height);
            }
        }

        Window.Present();
    }

    public int LastHeight { get; set; }
    public int LastWidth { get; set; }
    public int LastTop { get; set; }
    public int LastLeft { get; set; }
    public Application App { get; set; }
    public Window? Window { get; set; }
    public PhotinoInitParams InitParams { get; set; }

    private WebView? _webView { get; set; }

    // private Geometry _hints { get; set; }
    private bool _isFullScreen { get; set; }

    public void SetWebkitSettings()
    {
        var settings = _webView.GetSettings();

        settings.AllowModalDialogs = true; // default: false
        settings.AllowTopNavigationToDataUrls = true; // default: false
        settings.AllowUniversalAccessFromFileUrls = true; // default: false
        settings.EnableBackForwardNavigationGestures = true; // default: false
        settings.EnableMediaCapabilities = true; // default: false
        settings.EnableMockCaptureDevices = true; // default: false
        settings.EnablePageCache = true; // default: false
        settings.EnableWebrtc = true; // default: false
        settings.JavascriptCanOpenWindowsAutomatically = true; // default: false

        settings.AllowFileAccessFromFileUrls = _fileSystemAccessEnabled; // default: false
        settings.DisableWebSecurity = _webSecurityEnabled; // default: false
        settings.EnableDeveloperExtras = _devToolsEnabled; // default: false
        settings.EnableMediaStream = _mediaStreamEnabled; // default: false
        settings.EnableSmoothScrolling = _smoothScrollingEnabled; // default: true
        settings.JavascriptCanAccessClipboard = _javascriptClipboardAccessEnabled; // default: false
        settings.MediaPlaybackRequiresUserGesture = _mediaAutoplayEnabled; // default: false
        settings.UserAgent = _userAgent; // default: none

        if (!string.IsNullOrEmpty(_browserControlInitParameters))
        {
            SetWebkitCustomSettings(settings);
        }

    }

    public void SetWebkitCustomSettings(Settings settings)
    {
        Console.WriteLine("SetWebkitCustomSettings");
    }

    private void AddCustomSchemeHandlers()
    {
        foreach (var customSchemeName in _customSchemeNames)
        {
            _webView!.WebContext.RegisterUriScheme(customSchemeName, _onCustomScheme);
        }
    }

    private void HandleWebMessage(UserContentManager contentManager, UserContentManager.ScriptMessageReceivedSignalArgs args)
    {
        var jsValue = args.Value;

        if (jsValue.IsString())
        {
            var message = jsValue.ToString();

            _onWebMessageReceived?.Invoke(message);
        }
    }

    public override void ClearBrowserAutoFill()
    {
        // TODO: from Photino
    }

    public override void Close()
    {
        Window!.Close();
    }

    public override bool GetTransparentEnabled()
    {
        return _transparentEnabled;
    }

    public override bool GetContextMenuEnabled()
    {
        return ContextMenuEnabled;
    }

    public override bool GetDevToolsEnabled()
    {
        _devToolsEnabled = _webView.GetSettings().GetEnableDeveloperExtras();
        return _devToolsEnabled;
    }

    public override bool GetFullScreen()
    {
        return _isFullScreen;
    }

    public override bool GetGrantBrowserPermissions()
    {
        return _grantBrowserPermissions;
    }

    public override string GetUserAgent()
    {
        return _userAgent;
    }

    public override bool GetMediaAutoplayEnabled()
    {
        return _mediaAutoplayEnabled;
    }

    public override bool GetFileSystemAccessEnabled()
    {
        return _fileSystemAccessEnabled;
    }

    public override bool GetWebSecurityEnabled()
    {
        return _webSecurityEnabled;
    }

    public override bool GetJavascriptClipboardAccessEnabled()
    {
        return _javascriptClipboardAccessEnabled;
    }

    public override bool GetMediaStreamEnabled()
    {
        return _mediaStreamEnabled;
    }

    public override bool GetSmoothScrollingEnabled()
    {
        return _smoothScrollingEnabled;
    }

    public override bool GetNotificationsEnabled()
    {
        return _notificationsEnabled;
    }

    public override string GetIconFileName()
    {
        return _iconFileName;
    }

    public override bool GetMaximized()
    {
        return Window!.IsMaximized();
    }

    public override bool GetMinimized()
    {
        return (Window!.GetStateFlags() & StateFlags.Prelight) != 0;
    }

    public override bool GetResizable()
    {
        return Window!.Resizable;
    }

    public override uint GetScreenDpi()
    {
        var display = Window?.GetDisplay();
        if (display is null)
        {
            return 96;
        }

        var monitors = display.GetMonitors();
        if (monitors.GetNItems() == 0)
        {
            return 96;
        }

        var monitorPtr = monitors.GetItem(0);
        if (monitorPtr == IntPtr.Zero)
        {
            return 96;
        }

        var montior = new Gdk.Monitor(new MonitorHandle(monitorPtr, false));
        var scaleFactor = montior.GetScaleFactor();

        return (uint) (scaleFactor * 96);
    }

    public override Size GetSize()
    {
        var width = Window?.GetWidth() ?? 0;
        var height = Window?.GetHeight() ?? 0;

        return new Size()
        {
            Width = width,
            Height = height
        };
    }

    public override string GetTitle()
    {
        return Window!.GetTitle();
    }

    public override bool GetTopmost()
    {
        return (Window?.GetStateFlags() & StateFlags.Focused) != 0;
    }

    public override int GetZoom()
    {
        return _zoom;
    }

    public override bool GetIgnoreCertificateErrorsEnabled()
    {
        return _ignoreCertificateErrorsEnabled;
    }

    public override void NavigateToString(string content)
    {
        _webView!.LoadHtml(content, string.Empty);
    }

    public override void NavigateToUrl(string url)
    {
        _webView!.LoadUri(url);
    }

    public override void Restore()
    {
        Window?.Present();
    }

    public override void SendWebMessage(string message)
    {
        var sb = new StringBuilder();
        sb.Append("__dispatchMessageCallback(\"");
        sb.Append(JsonSerializer.Serialize(message));
        sb.Append("\")");

        _webView!.EvaluateJavascriptAsync(sb.ToString()).GetAwaiter();
    }

    public override void SetTransparentEnabled(bool enabled)
    {
        Window?.Decorated = enabled;
    }

    public override void SetContextMenuEnabled(bool enabled)
    {
        ContextMenuEnabled = enabled;
    }

    public override void SetDevToolsEnabled(bool enabled)
    {
        Console.WriteLine("setDevToolsEnabled");
        _devToolsEnabled = enabled;
        _webView!.GetSettings().EnableDeveloperExtras = _devToolsEnabled;
    }

    public override void SetIconFile(string filename)
    {
        throw new NotImplementedException();
    }

    public override void SetFullScreen(bool fullScreen)
    {
        if (fullScreen)
        {
            Window?.Fullscreen();
        }
        else
        {
            Window?.Unfullscreen();
        }

        _isFullScreen = fullScreen;
    }

    public override void SetMaximized(bool maximized)
    {
        if (maximized)
        {
            Window?.Maximize();
        }
        else
        {
            Window?.Unmaximize();
        }

        _isFullScreen = maximized;
    }

    public override void SetMinimized(bool minimized)
    {
        if (minimized)
        {
            Window?.Minimize();
        }
        else
        {
            Window?.Unminimize();
        }
    }

    public override void SetResizable(bool resizable)
    {
        Window?.Resizable = resizable;
    }

    public override void SetSize(Size size)
    {
        Window!.SetDefaultSize(size.Width, size.Height);
    }

    public override void SetTitle(string title)
    {
        Window?.SetTitle(title);
    }

    public override void SetTopmost(bool topmost)
    {
        Window!.SetStateFlags(StateFlags.Focused, !topmost);
    }

    public override void SetZoom(int zoom)
    {
        _zoom = zoom;
        _webView!.SetZoomLevel(_zoom);
    }

    public override void ShowNotification(string title, string message)
    {
        throw new NotImplementedException();
    }

    public override void WaitForExit()
    {
        App.RunWithSynchronizationContext(null);
    }

    public override void AddCustomSchemeName(string scheme)
    {
        throw new NotImplementedException();
    }

    public override List<Monitor> GetAllMonitors()
    {
        throw new NotImplementedException();
    }

    public override void SetClosingCallback(Func<bool> callback)
    {
        throw new NotImplementedException();
    }

    public override void SetFocusInCallback(Action callback)
    {
        throw new NotImplementedException();
    }

    public override void SetFocusOutCallback(Action callback)
    {
        throw new NotImplementedException();
    }

    public override void SetMovedCallback(Action<int, int> callback)
    {
        throw new NotImplementedException();
    }

    public override void SetResizedCallback(Action<int, int> callback)
    {
        throw new NotImplementedException();
    }

    public override void SetMaximizedCallback(Action callback)
    {
        throw new NotImplementedException();
    }

    public override void SetRestoredCallback(Action callback)
    {
        throw new NotImplementedException();
    }

    public override void SetMinimizedCallback(Action callback)
    {
        throw new NotImplementedException();
    }

    public override void Invoke(Action callback)
    {
        throw new NotImplementedException();
    }
}
