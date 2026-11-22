using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using PhotinoEx.Core.Models;
using PhotinoEx.Core.Platform.Mac.Dialog;
using Monitor = PhotinoEx.Core.Models.Monitor;
using NativeWindow = PhotinoEx.Core.PhotinoExWindow;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PhotinoEx.Core.Platform.Mac;

[SupportedOSPlatform("macos")]
public sealed class MacPhotinoEx : PhotinoEx
{
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";
    private readonly NativeWindow _window;

    public MacPhotinoEx(PhotinoExInitParams parameters)
    {
        _parent = parameters.ParentInstance;
        _customSchemeCallback = parameters.CustomSchemeHandler;
        _WebMessageReceivedCallback = parameters.WebMessageRecievedHandler;
        _WebMessageReceivedWithSourceCallback = parameters.WebMessageReceivedWithSourceHandler;
        _filesDroppedCallback = parameters.FilesDroppedHandler;
        _closingCallback = parameters.ClosingHandler;
        _focusInCallback = parameters.FocusInHandler;
        _focusOutCallback = parameters.FocusOutHandler;
        _resizedCallback = parameters.ResizedHandler;
        _movedCallback = parameters.MovedHandler;
        _maximizedCallback = parameters.MaximizedHandler;
        _restoredCallback = parameters.RestoredHandler;
        _minimizedCallback = parameters.MinimizedHandler;

        _window = new NativeWindow((parameters.ParentInstance as MacPhotinoEx)?._window)
            .SetTitle(parameters.Title)
            .SetChromeless(parameters.Chromeless)
            .SetTransparent(parameters.Transparent)
            .SetContextMenuEnabled(parameters.ContextMenuEnabled)
            .SetDevToolsEnabled(parameters.DevToolsEnabled)
            .SetFullScreen(parameters.FullScreen)
            .SetGrantBrowserPermissions(parameters.GrantBrowserPermissions)
            .SetUserAgent(parameters.UserAgent)
            .SetMediaAutoplayEnabled(parameters.MediaAutoplayEnabled)
            .SetFileSystemAccessEnabled(parameters.FileSystemAccessEnabled)
            .SetWebSecurityEnabled(parameters.WebSecurityEnabled)
            .SetJavascriptClipboardAccessEnabled(parameters.JavascriptClipboardAccessEnabled)
            .SetMediaStreamEnabled(parameters.MediaStreamEnabled)
            .SetSmoothScrollingEnabled(parameters.SmoothScrollingEnabled)
            .SetIgnoreCertificateErrorsEnabled(parameters.IgnoreCertificateErrorsEnabled)
            .SetNotificationsEnabled(parameters.NotificationsEnabled)
            .SetResizable(parameters.Resizable)
            .SetTopMost(parameters.Topmost)
            .SetUseOsDefaultLocation(parameters.UseOsDefaultLocation)
            .SetUseOsDefaultSize(parameters.UseOsDefaultSize)
            .SetZoom(parameters.Zoom)
            .SetMinSize(parameters.MinWidth, parameters.MinHeight)
            .SetMaxSize(Math.Min(parameters.MaxWidth, 10_000), Math.Min(parameters.MaxHeight, 10_000));

        if (!parameters.UseOsDefaultSize)
        {
            _window.SetSize(parameters.Width, parameters.Height);
        }

        if (!parameters.UseOsDefaultLocation)
        {
            _window.SetLocation(new Point(parameters.Left, parameters.Top));
        }

        if (parameters.CenterOnInitialize)
        {
            _window.Centered = true;
        }

        if (parameters.Maximized)
        {
            _window.SetMaximized(true);
        }
        else if (parameters.Minimized)
        {
            _window.SetMinimized(true);
        }

        if (!string.IsNullOrWhiteSpace(parameters.WindowIconFile))
        {
            _window.SetIconFile(parameters.WindowIconFile);
        }

        if (!string.IsNullOrWhiteSpace(parameters.BrowserControlInitParameters))
        {
            _window.SetBrowserControlInitParameters(parameters.BrowserControlInitParameters);
        }

        if (!string.IsNullOrWhiteSpace(parameters.StartUrl))
        {
            _window.Load(new Uri(parameters.StartUrl));
        }
        else
        {
            _window.LoadRawString(parameters.StartString);
        }

        foreach (var scheme in parameters.CustomSchemeNames ?? [])
        {
            AddCustomSchemeName(scheme);
        }

        _window.RegisterWebMessageReceivedHandler(
            (_, message) =>
            {
                _WebMessageReceivedWithSourceCallback?.Invoke(null, message);
                _WebMessageReceivedCallback?.Invoke(message);
            }
        );
        _window.RegisterWindowClosingHandler((_, _) => _closingCallback?.Invoke() ?? false);
        _window.RegisterFocusInHandler((_, _) => _focusInCallback?.Invoke());
        _window.RegisterFocusOutHandler((_, _) => _focusOutCallback?.Invoke());
        _window.RegisterLocationChangedHandler((_, point) => _movedCallback?.Invoke(point.X, point.Y));
        _window.RegisterSizeChangedHandler((_, size) => _resizedCallback?.Invoke(size.Width, size.Height));
        _window.RegisterMaximizedHandler((_, _) => _maximizedCallback?.Invoke());
        _window.RegisterRestoredHandler((_, _) => _restoredCallback?.Invoke());
        _window.RegisterMinimizedHandler((_, _) => _minimizedCallback?.Invoke());

        Dialog = new MacPhotinoExDialog(_window);
    }

    [UnsupportedOSPlatform("macos")]
    public override void ClearBrowserAutoFill() =>
        throw new PlatformNotSupportedException("Clearing browser autofill is not implemented by the macOS WebKit backend.");

    public override void SetClipboardText(string text)
    {
        var pasteboard = Send(GetClass("NSPasteboard"), GetSelector("generalPasteboard"));
        Send(pasteboard, GetSelector("clearContents"));

        var nsString = GetClass("NSString");
        var value = Send(nsString, GetSelector("stringWithUTF8String:"), text);
        var type = Send(nsString, GetSelector("stringWithUTF8String:"), "public.utf8-plain-text");
        if (!Send(pasteboard, GetSelector("setString:forType:"), value, type))
        {
            throw new InvalidOperationException("macOS rejected the clipboard content.");
        }
    }

    public override void SetClipboardFiles(IReadOnlyList<string> paths)
    {
        var objects = Send(GetClass("NSMutableArray"), GetSelector("array"));
        var nsString = GetClass("NSString");
        var nsUrl = GetClass("NSURL");
        foreach (var path in paths)
        {
            var value = Send(nsString, GetSelector("stringWithUTF8String:"), path);
            var url = SendObject(nsUrl, GetSelector("fileURLWithPath:"), value);
            SendObject(objects, GetSelector("addObject:"), url);
        }

        var pasteboard = Send(GetClass("NSPasteboard"), GetSelector("generalPasteboard"));
        Send(pasteboard, GetSelector("clearContents"));
        if (!SendBoolObject(pasteboard, GetSelector("writeObjects:"), objects))
        {
            throw new InvalidOperationException("macOS rejected the clipboard file list.");
        }
    }

    [UnsupportedOSPlatform("macos")]
    public override Task<FileDragDropEffects> BeginFileDragAsync(
        IReadOnlyList<string> paths,
        FileDragDropEffects allowedEffects,
        CancellationToken cancellationToken
    ) => throw new PlatformNotSupportedException("Outbound file dragging is not implemented on macOS.");

    public override void Close() => _window.Close();

    public override bool GetTransparentEnabled() => _window.Transparent;

    public override bool GetContextMenuEnabled() => _window.ContextMenuEnabled;

    public override bool GetDevToolsEnabled() => _window.DevToolsEnabled;

    public override bool GetFullScreen() => _window.FullScreen;

    public override bool GetGrantBrowserPermissions() => _window.GrantBrowserPermissions;

    public override string GetUserAgent() => _window.UserAgent;

    public override bool GetMediaAutoplayEnabled() => _window.MediaAutoplayEnabled;

    public override bool GetFileSystemAccessEnabled() => _window.FileSystemAccessEnabled;

    public override bool GetWebSecurityEnabled() => _window.WebSecurityEnabled;

    public override bool GetJavascriptClipboardAccessEnabled() => _window.JavascriptClipboardAccessEnabled;

    public override bool GetMediaStreamEnabled() => _window.MediaStreamEnabled;

    public override bool GetSmoothScrollingEnabled() => _window.SmoothScrollingEnabled;

    public override bool GetNotificationsEnabled() => _window.NotificationsEnabled;

    public override string GetIconFileName() => _window.IconFile ?? "";

    public override bool GetMaximized() => _window.Maximized;

    public override bool GetMinimized() => _window.Minimized;

    public override bool GetResizable() => _window.Resizable;

    public override uint GetScreenDpi() => _window.ScreenDpi;

    public override Size GetSize() => _window.Size;

    public override string GetTitle() => _window.Title;

    public override bool GetTopmost() => _window.Topmost;

    public override int GetZoom() => _window.Zoom;

    public override bool GetIgnoreCertificateErrorsEnabled() => _window.IgnoreCertificateErrorsEnabled;

    public override Point GetPosition() => _window.Location;

    public override void NavigateToString(string content) => _window.LoadRawString(content);

    public override void NavigateToUrl(string url) => _window.Load(new Uri(url));

    public override void Restore()
    {
        if (_window.Minimized)
            _window.SetMinimized(false);
        if (_window.Maximized)
            _window.SetMaximized(false);
    }

    public override void SendWebMessage(string message) => _window.SendWebMessageAsync(message).GetAwaiter().GetResult();

    public override void SetTransparentEnabled(bool enabled) => _window.SetTransparent(enabled);

    public override void SetContextMenuEnabled(bool enabled) => _window.SetContextMenuEnabled(enabled);

    public override void SetDevToolsEnabled(bool enabled) => _window.SetDevToolsEnabled(enabled);

    public override void SetPosition(Point newLocation) => _window.SetLocation(newLocation);

    public override void SetIconFile(string filename) => _window.SetIconFile(filename);

    public override void SetFullScreen(bool fullScreen) => _window.SetFullScreen(fullScreen);

    public override void SetMaximized(bool maximized) => _window.SetMaximized(maximized);

    public override void SetMinimized(bool minimized) => _window.SetMinimized(minimized);

    public override void SetResizable(bool resizable) => _window.SetResizable(resizable);

    public override void SetSize(Size size) => _window.SetSize(size);

    public override void SetTitle(string title) => _window.SetTitle(title);

    public override void SetTopmost(bool topmost) => _window.SetTopMost(topmost);

    public override void SetZoom(int zoom) => _window.SetZoom(zoom);

    public override void ShowNotification(string title, string message) =>
        _window.SendNotificationAsync(title, message).GetAwaiter().GetResult();

    public override void WaitForExit() => _window.WaitForClose();

    public override void AddCustomSchemeName(string scheme)
    {
        if (_customSchemeNames.Contains(scheme))
            return;
        _customSchemeNames.Add(scheme);
        _window.RegisterCustomSchemeHandler(scheme, HandleCustomSchemeRequest);
    }

    public override List<Monitor> GetAllMonitors() =>
        _window
            .Monitors.Select(m => new Monitor(
                new MonitorRect
                {
                    X = m.MonitorArea.X,
                    Y = m.MonitorArea.Y,
                    Width = m.MonitorArea.Width,
                    Height = m.MonitorArea.Height,
                },
                new MonitorRect
                {
                    X = m.WorkArea.X,
                    Y = m.WorkArea.Y,
                    Width = m.WorkArea.Width,
                    Height = m.WorkArea.Height,
                },
                m.Scale
            ))
            .ToList();

    public override void SetClosingCallback(Func<bool> callback) => _closingCallback = callback;

    public override void SetFocusInCallback(Action callback) => _focusInCallback = callback;

    public override void SetFocusOutCallback(Action callback) => _focusOutCallback = callback;

    public override void SetMovedCallback(Action<int, int> callback) => _movedCallback = callback;

    public override void SetResizedCallback(Action<int, int> callback) => _resizedCallback = callback;

    public override void SetMaximizedCallback(Action callback) => _maximizedCallback = callback;

    public override void SetRestoredCallback(Action callback) => _restoredCallback = callback;

    public override void SetMinimizedCallback(Action callback) => _minimizedCallback = callback;

    public override void Invoke(Action callback) => _window.Invoke(callback);

    private Stream HandleCustomSchemeRequest(object? sender, string? scheme, string url, out string contentType) =>
        _customSchemeCallback?.Invoke(url, out contentType)
        ?? throw new InvalidOperationException($"No handler is registered for the '{scheme}' scheme.");

    private static IntPtr GetClass(string name) => objc_getClass(name);

    private static IntPtr GetSelector(string name) => sel_registerName(name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, string value);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendObject(IntPtr receiver, IntPtr selector, IntPtr value);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SendBoolObject(IntPtr receiver, IntPtr selector, IntPtr value);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Send(IntPtr receiver, IntPtr selector, IntPtr value, IntPtr type);
}
