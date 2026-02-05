using Gdk;
using Gtk;
using PhotinoEx.Core.TempModels;
using Action = System.Action;
using Monitor = PhotinoEx.Core.Models.Monitor;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;

namespace PhotinoEx.Core.Platform.Linux;

public class LPhotino : Photino
{
    public LPhotino(PhotinoInitParams initParams)
    {
        _windowTitle = string.IsNullOrEmpty(initParams.Title) ? "Set a title" : initParams.Title;
        _startUrl = initParams.StartUrl;
        _startString = initParams.StartString;
        _temporaryFilesPath = initParams.TemporaryFilesPath;
        _userAgent = initParams.UserAgent;
        _browserControlInitParameters = initParams.BrowserControlInitParameters;

        _transparentEnabled = initParams.Transparent;
        _devToolsEnabled = initParams.DevToolsEnabled;
        _grantBrowserPermissions = initParams.GrantBrowserPermissions;
        _mediaAutoplayEnabled = initParams.MediaAutoplayEnabled;
        _fileSystemAccessEnabled = initParams.FileSystemAccessEnabled;
        _webSecurityEnabled = initParams.WebSecurityEnabled;
        _javascriptClipboardAccessEnabled = initParams.JavascriptClipboardAccessEnabled;
        _mediaStreamEnabled = initParams.MediaStreamEnabled;
        _smoothScrollingEnabled = initParams.SmoothScrollingEnabled;
        _ignoreCertificateErrorsEnabled = initParams.IgnoreCertificateErrorsEnabled;
        _isFullScreen = initParams.FullScreen;

        ContextMenuEnabled = initParams.ContextMenuEnabled;

        _zoom = initParams.Zoom;
        MinWidth = initParams.MinWidth;
        MinWidth = initParams.MinHeight;
        MaxWidth = initParams.MaxWidth;
        MaxHeight = initParams.MaxHeight;

        _onWebMessageReceived = initParams.OnWebMessageReceived;
        _onResized = initParams.OnResized;
        _onMoved = initParams.OnMoved;
        _onClosing = initParams.OnClosing;
        _onFocusIn = initParams.OnFocusIn;
        _onFocusOut = initParams.OnFocusOut;
        _onMaximized = initParams.OnMaximized;
        _onMinimized = initParams.OnMinimized;
        _onRestored = initParams.OnRestored;
        _onCustomScheme = initParams.OnCustomScheme; // TODO: this is not correct, but deal with later

        if (initParams.CustomSchemeNames?.Count > 16)
        {
            throw new ApplicationException("too many custom schemes, 16 max");
        }

        foreach (var schemes in initParams.CustomSchemeNames)
        {
            _customSchemeNames.Add(schemes);
        }

        _parent = initParams.ParentInstance;
        Window = new Window(WindowType.Toplevel);
        _dialog = new LPhotinoDialog();

        if (initParams.FullScreen)
        {
            SetFullScreen(true);
        }
        else
        {
            if (initParams.Width > initParams.MaxWidth)
            {
                initParams.Width = initParams.MaxWidth;
            }

            if (initParams.Height > initParams.MaxHeight)
            {
                initParams.Height = initParams.MaxHeight;
            }

            if (initParams.Width < initParams.MinWidth)
            {
                initParams.Width = initParams.MinWidth;
            }

            if (initParams.Height < initParams.MinWidth)
            {
                initParams.Height = initParams.MinWidth;
            }

            if (initParams.UseOsDefaultSize)
            {
                // gtk_window_set_default_size(GTK_WINDOW(_window), -1, -1);
                Window.SetDefaultSize(-1, -1);
            }
            else
            {
                // gtk_window_set_default_size(GTK_WINDOW(_window), initParams->Width, initParams->Height);
                Window.SetDefaultSize(initParams.Width, initParams.Height);
            }

            SetMinSize(new Size()
            {
                Height = initParams.MinHeight,
                Width = initParams.MinWidth
            });
            SetMaxSize(new Size()
            {
                Height = initParams.MaxHeight,
                Width = initParams.MaxWidth
            });

            if (initParams.UseOsDefaultLocation)
            {
                // gtk_window_set_position(GTK_WINDOW(_window), GTK_WIN_POS_NONE);
                Window.SetPosition(WindowPosition.None);
            }
            else if (initParams.CenterOnInitialize && !initParams.FullScreen)
            {
                // gtk_window_set_position(GTK_WINDOW(_window), GTK_WIN_POS_CENTER);
                Window.SetPosition(WindowPosition.Center);
            }
            else
            {
                // gtk_window_move(GTK_WINDOW(_window), initParams->Left, initParams->Top);
                Window.Move(initParams.Left, initParams.Top);
            }
        }

        SetTitle(_windowTitle);

        if (initParams.Chromeless)
        {
            // gtk_window_set_decorated(GTK_WINDOW(_window), false);
            Window.Decorated = false;
        }

        if (!string.IsNullOrEmpty(initParams.WindowIconFile))
        {
            SetIconFile(initParams.WindowIconFile);
        }

        if (initParams.CenterOnInitialize)
        {
            Center();
        }

        if (initParams.Minimized)
        {
            SetMinimized(true);
        }

        if (initParams.Maximized)
        {
            SetMaximized(true);
        }

        if (!initParams.Resizable)
        {
            SetResizable(false);
        }

        if (initParams.Topmost)
        {
            SetTopmost(true);
        }

        Window.FocusInEvent += OnFocusInEvent;
        Window.FocusOutEvent += OnFocusOutEvent;
        Window.WindowStateEvent += (object o, WindowStateEventArgs args) =>
        {
            Console.WriteLine($"WindowStateChange: {args.Event.ChangedMask}");
        };
        Window.DeleteEvent += (object o, DeleteEventArgs args) =>
        {
            Console.WriteLine($"WindowDeleteEvent: {args.Event.Type}");
            Application.Quit();
            args.RetVal = true;
        };

        // These must be called after the webview control is initialized.
        // g_signal_connect(G_OBJECT(_webview), "context-menu",
        //     G_CALLBACK(on_webview_context_menu),
        //     this);
        //
        // g_signal_connect(G_OBJECT(_webview), "permission-request",
        //     G_CALLBACK(on_permission_request),
        //     this);

        AddCustomSchemeHandlers();

        if (initParams.Transparent)
        {
            SetTransparentEnabled(true);
        }

        if (_zoom != 100)
        {
            SetZoom(_zoom);
        }
    }

    private void OnFocusOutEvent(object o, FocusOutEventArgs args)
    {
        Console.WriteLine($"FocusOut: {args.Event.Type}");
    }

    private void OnFocusInEvent(object o, FocusInEventArgs args)
    {
        Console.WriteLine($"FocusIn: {args.Event.Type}");
    }

    public int LastHeight { get; set; }
    public int LastWidth { get; set; }
    public int LastTop { get; set; }
    public int LastLeft { get; set; }
    public Window? Window { get; set; }

    private Widget? _webView { get; set; }
    private Geometry _hints { get; set; }
    private bool _isFullScreen { get; set; }

    public void SetWebkitSettings()
    {
        throw new NotImplementedException();
    }

    public void SetWebkitCustomSettings(WebkitSettings settings)
    {
        throw new NotImplementedException();
    }

    private void AddCustomSchemeHandlers()
    {
        Console.WriteLine("AddCustomSchemeHandlers");
    }

    public override void Show()
    {
        if (_webView is null)
        {
            // Webkit stuff
            var contentManager = new WebKit.UserContentManager();

            _webView = new WebKit.WebView(contentManager);

            SetWebkitSettings();

            Window.Add(_webView);

            string script = @"
                window.__receiveMessageCallbacks = [];
                window.__dispatchMessageCallback = function(message) {
                    window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });
                };
                window.external = {
                    sendMessage: function(message) {
                        window.webkit.messageHandlers.Photinointerop.postMessage(message);
                    },
                receiveMessage: function(callback) {
                    window.__receiveMessageCallbacks.push(callback);
                }
            ";

            // var userScript = new WebKit.UserScript(
            //     script,
            //     WebKit.UserContentInjectedFrames.AllFrames,
            //     WebKit.UserScriptInjectionTime.Start,
            //     null,  // whitelist
            //     null   // blacklist
        }

        Window!.ShowAll();
    }

    public override void Center()
    {
        // check this works
        Window.SetPosition(WindowPosition.Center);
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
        throw new NotImplementedException();
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
        // TODO: check if this works from photino
        //gboolean maximized = gtk_window_is_maximized(GTK_WINDOW(_window));  //this method doesn't work
        //*isMaximized = maximized;

        return Window!.IsMaximized;
    }

    public override bool GetMinimized()
    {
        // TODO: check this works
        return (Window!.StateFlags & StateFlags.Prelight) != 0;
    }

    public override Point GetPosition()
    {
        var x = 0;
        var y = 0;
        Window?.GetPosition(out x, out y);

        return new Point()
        {
            X = x,
            Y = y
        };
    }

    public override bool GetResizable()
    {
        return Window!.Resizable;
    }

    public override uint GetScreenDpi()
    {
        var dpi = Window?.Screen.Resolution ?? 0D;
        return dpi < 0 ? 96u : (uint) dpi;
    }

    public override Size GetSize()
    {
        var width = 0;
        var height = 0;
        Window?.GetSize(out width, out height);

        return new Size()
        {
            Width = width,
            Height = height
        };
    }

    public override string GetTitle()
    {
        return Window!.Title;
    }

    public override bool GetTopmost()
    {
        return (Window?.StateFlags & StateFlags.Focused) != 0;
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
        Console.WriteLine($"Navigating to string:{content}");
    }

    public override void NavigateToUrl(string url)
    {
        Console.WriteLine($"Navigating to url:{url}");
    }

    public override void Restore()
    {
        Window?.Present();
    }

    public override void SendWebMessage(string message)
    {
        throw new NotImplementedException();
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
        _devToolsEnabled = enabled;
        // TODO: finish this off
        throw new NotImplementedException();
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

    public override void SetMaxSize(Size size)
    {
        _hints = _hints with
        {
            MaxWidth = size.Width,
            MaxHeight = size.Height
        };

        Window?.SetGeometryHints(Window, _hints, WindowHints.MinSize | WindowHints.MaxSize);
    }

    public override void SetMinimized(bool minimized)
    {
        if (minimized)
        {
            Window?.Iconify();
        }
        else
        {
            Window?.Deiconify();
        }
    }

    public override void SetMinSize(Size size)
    {
        _hints = _hints with
        {
            MinWidth = size.Width,
            MinHeight = size.Height
        };

        Window?.SetGeometryHints(Window, _hints, WindowHints.MinSize | WindowHints.MaxSize);
    }

    public override void SetPosition(Point position)
    {
        Window?.Move(position.X, position.Y);
    }

    public override void SetResizable(bool resizable)
    {
        Window?.Resizable = resizable;
    }

    public override void SetSize(Size size)
    {
        Window!.Resize(size.Width, size.Height);
    }

    public override void SetTitle(string title)
    {
        Window?.Title = title;
    }

    public override void SetTopmost(bool topmost)
    {
        Window?.KeepAbove = topmost;
    }

    public override void SetZoom(int zoom)
    {
        throw new NotImplementedException();
    }

    public override void ShowNotification(string title, string message)
    {
        throw new NotImplementedException();
    }

    public override void WaitForExit()
    {
        Application.Run();
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
