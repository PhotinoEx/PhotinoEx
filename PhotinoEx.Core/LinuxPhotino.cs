using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Gdk.Internal;
using Gio;
using GLib;
using GObject;
using Gtk;
using PhotinoEx.Core.Enums;
using PhotinoEx.Core.Models;
using WebKit;
using Action = System.Action;
using Application = Gtk.Application;
using ApplicationWindow = Gtk.ApplicationWindow;
using File = Gio.File;
using FileDialog = Gtk.FileDialog;
using FileFilter = Gtk.FileFilter;
using MessageDialog = Gtk.MessageDialog;
using Monitor = PhotinoEx.Core.Models.Monitor;
using Notification = Gio.Notification;
using Window = Gtk.Window;
using Size = System.Drawing.Size;
using Settings = WebKit.Settings;

namespace PhotinoEx.Core;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
public class LinuxPhotino : Photino
{
    public LinuxPhotino(PhotinoInitParams parameters)
    {
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

        _params = parameters;

        _windowTitle = string.IsNullOrEmpty(_params.Title) ? "Set a title" : _params.Title;
        _startUrl = _params.StartUrl;
        _startString = _params.StartString;
        _temporaryFilesPath = _params.TemporaryFilesPath;
        _userAgent = _params.UserAgent;
        _browserControlInitParameters = _params.BrowserControlInitParameters;

        _transparentEnabled = _params.Transparent;
        _devToolsEnabled = _params.DevToolsEnabled;
        _grantBrowserPermissions = _params.GrantBrowserPermissions;
        _mediaAutoplayEnabled = _params.MediaAutoplayEnabled;
        _fileSystemAccessEnabled = _params.FileSystemAccessEnabled;
        _webSecurityEnabled = _params.WebSecurityEnabled;
        _javascriptClipboardAccessEnabled = _params.JavascriptClipboardAccessEnabled;
        _mediaStreamEnabled = _params.MediaStreamEnabled;
        _smoothScrollingEnabled = _params.SmoothScrollingEnabled;
        _ignoreCertificateErrorsEnabled = _params.IgnoreCertificateErrorsEnabled;
        _isFullScreen = _params.FullScreen;

        ContextMenuEnabled = _params.ContextMenuEnabled;

        _zoom = _params.Zoom;
        MinWidth = _params.MinWidth;
        MinWidth = _params.MinHeight;
        MaxWidth = _params.MaxWidth;
        MaxHeight = _params.MaxHeight;

        _WebMessageReceivedCallback = _params.WebMessageRecievedHandler;
        _resizedCallback = _params.ResizedHandler;
        _movedCallback = _params.MovedHandler;
        _closingCallback = _params.ClosingHandler;
        _focusInCallback = _params.FocusInHandler;
        _focusOutCallback = _params.FocusOutHandler;
        _maximizedCallback = _params.MaximizedHandler;
        _minimizedCallback = _params.MinimizedHandler;
        _restoredCallback = _params.RestoredHandler;
        _customSchemeCallback = _params.CustomSchemeHandler;


        if (_params.CustomSchemeNames?.Count > 16)
        {
            throw new ApplicationException("too many custom schemes, 16 max");
        }

        foreach (var schemes in _params.CustomSchemeNames ?? [])
        {
            _customSchemeNames.Add(schemes);
        }

        _parent = _params.ParentInstance;
        _application = Application.New($"com.photinoex.App", ApplicationFlags.FlagsNone);
        WebKit.Module.Initialize();
        _application.OnActivate += ApplicationOnActivate;
    }

    private void ApplicationOnActivate(Gio.Application sender, EventArgs args)
    {
        _webView = WebView.New();
        _webView.HeightRequest = 500;
        _webView.WidthRequest = 500;

        AddCustomSchemeHandlers();
        SetWebkitSettings();
        var contentManager = _webView.GetUserContentManager();

        var scriptSource = @"
            window.__receiveMessageCallbacks = [];
            window.__dispatchMessageCallback = function(message) {
                window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });
            };
            window.external = {
                sendMessage: function(message) {
                    console.log(message);
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
        contentManager.RegisterScriptMessageHandler("Photinointerop", null);
        contentManager.OnScriptMessageReceived += HandleWebMessage;

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

        _window = ApplicationWindow.New((Application) sender);
        _window!.SetChild(_webView);

        if (_params.FullScreen)
        {
            _window.Fullscreen();
        }
        else
        {
            if (_params.Width > _params.MaxWidth)
            {
                _params.Width = _params.MaxWidth;
            }

            if (_params.Height > _params.MaxHeight)
            {
                _params.Height = _params.MaxHeight;
            }

            if (_params.Width < _params.MinWidth)
            {
                _params.Width = _params.MinWidth;
            }

            if (_params.Height < _params.MinWidth)
            {
                _params.Height = _params.MinWidth;
            }

            if (_params.UseOsDefaultSize)
            {
                _window.SetDefaultSize(-1, -1);
            }
            else
            {
                _window.SetDefaultSize(_params.Width, _params.Height);
            }
        }

        // TODO: fix this
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

        SetTitle("test");

        if (_params.Chromeless)
        {
            _window!.SetDecorated(false);
        }

        if (!string.IsNullOrEmpty(_params.WindowIconFile))
        {
            SetIconFile(_params.WindowIconFile);
        }

        if (_params.Minimized)
        {
            SetMinimized(true);
        }

        if (_params.Maximized)
        {
            SetMaximized(true);
        }

        if (!_params.Resizable)
        {
            SetResizable(false);
        }

        if (_params.Topmost)
        {
            SetTopmost(true);
        }

        if (_params.Transparent)
        {
            SetTransparentEnabled(true);
        }

        if (_zoom != 100)
        {
            SetZoom(_zoom);
        }

        _window.Present();
    }

    private Application _application { get; set; }
    private Window? _window { get; set; }
    private PhotinoInitParams _params { get; set; }
    private SynchronizationContext _syncContext;
    private WebView? _webView { get; set; }
    private bool _isFullScreen { get; set; }

    public void SetWebkitSettings()
    {
        var settings = _webView!.GetSettings();

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

        if (_ignoreCertificateErrorsEnabled)
        {
            _webView!.NetworkSession!.SetTlsErrorsPolicy(TLSErrorsPolicy.Ignore);
        }
        else
        {
            _webView!.NetworkSession!.SetTlsErrorsPolicy(TLSErrorsPolicy.Fail);
        }

        _webView!.SetSettings(settings);
    }

    public void SetWebkitCustomSettings(Settings settings)
    {
        using var doc = JsonDocument.Parse(_browserControlInitParameters);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Init parameters must be a JSON object");
        }

        foreach (var property in root.EnumerateObject())
        {
            var propertyName = property.Name;
            var value = property.Value;

            using var gvalue = new Value();

            try
            {
                switch (value.ValueKind)
                {
                    case JsonValueKind.String:
                        gvalue.Init(GObject.Type.String);
                        gvalue.SetString(value.GetString());
                        break;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        gvalue.Init(GObject.Type.Boolean);
                        gvalue.SetBoolean(value.GetBoolean());
                        break;

                    case JsonValueKind.Number:
                        if (value.TryGetInt32(out int intValue))
                        {
                            gvalue.Init(GObject.Type.Int);
                            gvalue.SetInt(intValue);
                        }
                        else
                        {
                            gvalue.Init(GObject.Type.Double);
                            gvalue.SetDouble(value.GetDouble());
                        }

                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Invalid value type for key: {propertyName}"
                        );
                }
            }
            catch (Exception ex)
            {
                // Optional GTK dialog to match original behavior
                throw new Exception($"fucked up {ex}");
            }
        }
    }

    private void AddCustomSchemeHandlers()
    {
        foreach (var customSchemeName in _customSchemeNames)
        {
            _webView!.WebContext.RegisterUriScheme(customSchemeName, HandleCustomSchemeRequest);
        }
    }

    private void HandleCustomSchemeRequest(URISchemeRequest request)
    {
        var callback = _customSchemeCallback;
        var uri = request.GetUri();
        string contentType;

        var memoryStream = callback!.Invoke(uri, out contentType);
        var data = memoryStream.ToArray();

        var bytes = Bytes.New(data);
        var stream = MemoryInputStream.New();
        stream.AddBytes(bytes);

        request.Finish(
            stream,
            data.Length,
            contentType
        );
    }

    private void HandleWebMessage(UserContentManager contentManager, UserContentManager.ScriptMessageReceivedSignalArgs args)
    {
        var jsValue = args.Value;

        if (jsValue.IsString())
        {
            var message = jsValue.ToString();

            _WebMessageReceivedCallback?.Invoke(message);
        }
    }

    public override void ClearBrowserAutoFill()
    {
        // TODO: from Photino
    }

    public override void Close()
    {
        _window!.Close();
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

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("MacOs")]
    public override Point GetPosition()
    {
        return new Point(0, 0);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("MacOs")]
    public override void SetPosition(Point newLocation)
    {
        // do nothing, cuz
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
        return _window!.IsMaximized();
    }

    public override bool GetMinimized()
    {
        return (_window!.GetStateFlags() & StateFlags.Prelight) != 0;
    }

    public override bool GetResizable()
    {
        return _window!.Resizable;
    }

    public override uint GetScreenDpi()
    {
        var display = _window?.GetDisplay();
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
        var width = _window?.GetWidth() ?? 0;
        var height = _window?.GetHeight() ?? 0;

        return new Size()
        {
            Width = width,
            Height = height
        };
    }

    public override string GetTitle()
    {
        return _window?.GetTitle() ?? "";
    }

    public override bool GetTopmost()
    {
        return (_window?.GetStateFlags() & StateFlags.Focused) != 0;
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
        _webView?.LoadHtml(content, string.Empty);
    }

    public override void NavigateToUrl(string url)
    {
        _webView?.LoadUri(url);
    }

    public override void Restore()
    {
        _window?.Present();
    }

    public override void SendWebMessage(string message)
    {
        var sb = new StringBuilder();
        sb.Append("__dispatchMessageCallback(");
        sb.Append(JsonSerializer.Serialize(message));
        sb.Append(")");

        _webView?.EvaluateJavascriptAsync(sb.ToString()).GetAwaiter();
    }

    public override void SetTransparentEnabled(bool enabled)
    {
        _window?.Decorated = enabled;
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

    /// <summary>
    /// IMPORTANT: This does not work on Wayland, its ignored for the .desktop file, blame wayland
    /// </summary>
    /// <param name="filename"></param>
    public override void SetIconFile(string filename)
    {
        _window?.SetIconName(filename);
    }

    public override void SetFullScreen(bool fullScreen)
    {
        if (fullScreen)
        {
            _window?.Fullscreen();
        }
        else
        {
            _window?.Unfullscreen();
        }

        _isFullScreen = fullScreen;
    }

    public override void SetMaximized(bool maximized)
    {
        if (maximized)
        {
            _window?.Maximize();
        }
        else
        {
            _window?.Unmaximize();
        }

        _isFullScreen = maximized;
    }

    public override void SetMinimized(bool minimized)
    {
        if (minimized)
        {
            _window?.Minimize();
        }
        else
        {
            _window?.Unminimize();
        }
    }

    public override void SetResizable(bool resizable)
    {
        _window?.Resizable = resizable;
    }

    public override void SetSize(Size size)
    {
        _window!.SetDefaultSize(size.Width, size.Height);
    }

    public override void SetTitle(string title)
    {
        _window?.SetTitle(title);
    }

    public override void SetTopmost(bool topmost)
    {
        _window!.SetStateFlags(StateFlags.Focused, !topmost);
    }

    public override void SetZoom(int zoom)
    {
        _zoom = zoom;
        _webView!.SetZoomLevel(_zoom);
    }

    public override void ShowNotification(string title, string message)
    {
        // TODO: expand this to include icons/type of notification - e.g. error
        var notification = new Notification();
        notification.SetBody(message);
        notification.SetTitle(title);
        _application.SendNotification(null, notification);
    }

    public override void WaitForExit()
    {
        _application.RunWithSynchronizationContext(null);
    }

    public override void AddCustomSchemeName(string scheme)
    {
        _customSchemeNames.Add(scheme);
    }

    public override List<Monitor> GetAllMonitors()
    {
        var display = _window?.GetDisplay();
        if (display is null)
        {
            throw new Exception("Something went wrong getting display from GTK4");
        }

        var monitors = display.GetMonitors();
        var count = monitors.GetNItems();

        var monitorList = new List<Monitor>();

        if (count == 0)
        {
            return monitorList;
        }

        for (uint i = 0; i < count; i++)
        {
            // TODO: Test this returns what we actually want
            var monitorPtr = monitors.GetItem(i);
            var mont = new Gdk.Monitor(new MonitorHandle(monitorPtr, false));
            monitorList.Add(new Monitor(new MonitorRect()
            {
                Height = mont.HeightMm,
                Width = mont.WidthMm
            }, new MonitorRect()
            {
                Height = mont.HeightMm,
                Width = mont.WidthMm
            }, mont.Scale));
        }

        return monitorList;
    }

    public override void SetClosingCallback(Func<bool> callback)
    {
        _closingCallback = callback;
    }

    public override void SetFocusInCallback(Action callback)
    {
        _focusInCallback = callback;
    }

    public override void SetFocusOutCallback(Action callback)
    {
        _focusOutCallback = callback;
    }

    public override void SetMovedCallback(Action<int, int> callback)
    {
        _movedCallback = callback;
    }

    public override void SetResizedCallback(Action<int, int> callback)
    {
        _resizedCallback = callback;
    }

    public override void SetMaximizedCallback(Action callback)
    {
        _maximizedCallback = callback;
    }

    public override void SetRestoredCallback(Action callback)
    {
        _restoredCallback = callback;
    }

    public override void SetMinimizedCallback(Action callback)
    {
        _minimizedCallback = callback;
    }

    public override void Invoke(Action callback)
    {
        if (SynchronizationContext.Current == _syncContext)
        {
            callback();
            return;
        }

        _syncContext.Send(_ => callback(), null);
    }

    public override async Task<List<string>> ShowOpenFileAsync(string title, string? path, bool multiSelect, List<string>? filterPatterns)
    {
        var dialog = FileDialog.New();
        dialog.SetTitle(title);

        var filter = FileFilter.New();
        filter.Name = "FilterPatterns";
        foreach (var s in filterPatterns ?? new List<string>())
        {
            filter.AddPattern(s); // *.txt
        }

        var results = new List<string>();

        try
        {
            if (multiSelect)
            {
                var files = await dialog.OpenMultipleAsync(_window);

                for (uint i = 0; i < files?.GetNItems(); i++)
                {
                    var item = files.GetObject(i) as File;
                    if (item is null)
                    {
                        return results;
                    }

                    results.Add(item.GetPath()!);
                }
            }
            else
            {
                var file = await dialog.OpenAsync(_window);

                if (file is not null)
                {
                    var pathToUse = file.GetPath();
                    results.Add(pathToUse!);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return results;
    }

    /// <summary>
    /// TODO: GTK4 currently does not allow multiselect on folders. the api is there, just does not let me multiselect
    /// </summary>
    /// <param name="title"></param>
    /// <param name="path"></param>
    /// <param name="multiSelect"></param>
    /// <returns></returns>
    public override async Task<List<string>> ShowOpenFolderAsync(string title, string? path, bool multiSelect)
    {
        var dialog = FileDialog.New();
        dialog.SetTitle(title);

        var results = new List<string>();

        try
        {
            if (multiSelect)
            {
                var files = await dialog.SelectMultipleFoldersAsync(_window);

                for (uint i = 0; i < files?.GetNItems(); i++)
                {
                    var item = files.GetObject(i) as File;
                    if (item is null)
                    {
                        return results;
                    }

                    results.Add(item.GetPath()!);
                }
            }
            else
            {
                var file = await dialog.SelectFolderAsync(_window);

                if (file is not null)
                {
                    var pathToUse = file.GetPath();
                    results.Add(pathToUse!);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return results;
    }

    public override async Task<string> ShowSaveFileAsync(string title, string? path, List<string>? filterPatterns)
    {
        var dialog = FileDialog.New();
        dialog.SetTitle(title);

        var filter = FileFilter.New();
        filter.Name = "FilterPatterns";
        foreach (var s in filterPatterns ?? new List<string>())
        {
            filter.AddPattern(s); // *.txt
        }

        File? file = null;
        try
        {
            file = await dialog.SaveAsync(_window);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (file is null)
        {
            return "";
        }

        return file.GetPath()!;
    }

    public override async Task<DialogResult> ShowMessageAsync(string title, string text, DialogButtons buttons, DialogIcon icon)
    {
        var dialog = new MessageDialog();
        dialog.SetTitle(title);
        dialog.Text = text;
        dialog.SetModal(true);
        dialog.SetTransientFor(_window);

        switch (buttons)
        {
            case DialogButtons.Ok:
                {
                    dialog.AddButton("Ok", (int) DialogResult.Ok);
                    break;
                }
            case DialogButtons.OkCancel:
                {
                    dialog.AddButton("Ok", (int) DialogResult.Ok);
                    dialog.AddButton("Cancel", (int) DialogResult.Cancel);
                    break;
                }
            case DialogButtons.YesNo:
                {
                    dialog.AddButton("Yes", (int) DialogResult.Yes);
                    dialog.AddButton("No", (int) DialogResult.No);
                    break;
                }
            case DialogButtons.YesNoCancel:
                {
                    dialog.AddButton("Yes", (int) DialogResult.Yes);
                    dialog.AddButton("No", (int) DialogResult.No);
                    dialog.AddButton("Cancel", (int) DialogResult.Cancel);
                    break;
                }
            case DialogButtons.RetryCancel:
                {
                    dialog.AddButton("Retry", (int) DialogResult.Retry);
                    dialog.AddButton("Cancel", (int) DialogResult.Cancel);
                    break;
                }
            case DialogButtons.AbortRetryIgnore:
                {
                    dialog.AddButton("Abort", (int) DialogResult.Abort);
                    dialog.AddButton("Retry", (int) DialogResult.Retry);
                    dialog.AddButton("Cancel", (int) DialogResult.Cancel);
                    break;
                }
            default:
                dialog.AddButton("Ok", (int) DialogResult.Ok);
                break;
        }

        var tcs = new TaskCompletionSource<DialogResult>();

        dialog.OnResponse += (_, args) =>
        {
            switch (args.ResponseId)
            {
                case (int) ResponseType.Close:
                    tcs.SetResult(DialogResult.Cancel);
                    break;
                case (int) DialogResult.Ok:
                    tcs.SetResult(DialogResult.Ok);
                    break;
                case (int) DialogResult.Yes:
                    tcs.SetResult(DialogResult.Yes);
                    break;
                case (int) DialogResult.No:
                    tcs.SetResult(DialogResult.No);
                    break;
                case (int) DialogResult.Cancel:
                    tcs.SetResult(DialogResult.Cancel);
                    break;
                case (int) DialogResult.Abort:
                    tcs.SetResult(DialogResult.Abort);
                    break;
                case (int) DialogResult.Retry:
                    tcs.SetResult(DialogResult.Retry);
                    break;
                case (int) DialogResult.Ignore:
                    tcs.SetResult(DialogResult.Ignore);
                    break;
                default:
                    tcs.SetResult(DialogResult.Cancel);
                    break;
            }

            dialog.Destroy();
        };

        dialog.Present();

        return await tcs.Task;
    }
}
