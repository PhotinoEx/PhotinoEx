using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Web.WebView2.Core;
using PhotinoEx.Core.Enums;
using PhotinoEx.Core.Models;
using PhotinoEx.Core.Utils;
using Monitor = PhotinoEx.Core.Models.Monitor;
using Size = System.Drawing.Size;

namespace PhotinoEx.Core;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
public class WindowsPhotino : Photino
{
    public WindowsPhotino(PhotinoInitParams initParams)
    {
        darkBrush = DLLImports.CreateSolidBrush(RGB(0, 0, 0));
        lightBrush = DLLImports.CreateSolidBrush(RGB(255, 255, 255));

        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _params = initParams;

        _windowTitle = string.IsNullOrEmpty(_params.Title) ? "Set a title" : _params.Title;
        // they also did wintoast things here
        _startUrl = _params.StartUrl;
        _startString = _params.StartString;
        _temporaryFilesPath = _params.TemporaryFilesPath;
        _userAgent = _params.UserAgent;
        _browserControlInitParameters = _params.BrowserControlInitParameters;
        _notificationRegistrationId = _params.NotificationRegistrationId;
        _darkmodeEnabled = _params.Darkmode;

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

        ContextMenuEnabled = _params.ContextMenuEnabled;

        _zoom = _params.Zoom;
        MinWidth = _params.MinWidth;
        MinHeight = _params.MinHeight;
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

        if (_params.UseOsDefaultSize)
        {
            _params.Width = unchecked((int) Constants.CW_USEDEFAULT);
            _params.Height = unchecked((int) Constants.CW_USEDEFAULT);
        }
        else
        {
            if (_params.Width < 0)
            {
                _params.Width = unchecked((int) Constants.CW_USEDEFAULT);
            }

            if (_params.Height < 0)
            {
                _params.Height = unchecked((int) Constants.CW_USEDEFAULT);
            }
        }

        if (_params.UseOsDefaultLocation)
        {
            _params.Left = unchecked((int) Constants.CW_USEDEFAULT);
            _params.Top = unchecked((int) Constants.CW_USEDEFAULT);
        }

        if (_params.FullScreen)
        {
            _params.Left = 0;
            _params.Top = 0;
            _params.Width = DLLImports.GetSystemMetrics(Constants.SM_CXSCREEN);
            _params.Height = DLLImports.GetSystemMetrics(Constants.SM_CYSCREEN);
        }

        if (_params.Chromeless)
        {
            //CW_USEDEFAULT CAN NOT BE USED ON POPUP WINDOWS
            if (_params.Left == default && _params.Top == default)
            {
                _params.CenterOnInitialize = true;
            }

            if (_params.Left == default)
            {
                _params.Left = 0;
            }

            if (_params.Top == default)
            {
                _params.Top = 0;
            }

            if (_params.Height == default)
            {
                _params.Height = 600;
            }

            if (_params.Width == default)
            {
                _params.Width = 800;
            }
        }

        if (_params.Height > _params.MaxHeight)
        {
            _params.Height = _params.MaxHeight;
        }

        if (_params.Height < _params.MinHeight && _params.MinHeight > 0)
        {
            _params.Height = _params.MinHeight;
        }

        if (_params.Width > _params.MaxWidth)
        {
            _params.Width = _params.MaxWidth;
        }

        if (_params.Width < _params.MinWidth && _params.MinWidth > 0)
        {
            _params.Width = _params.MinWidth;
        }

        Register();

        _hwnd = DLLImports.CreateWindowEx(
            _params.Transparent ? Constants.WS_EX_LAYERED : 0,
            "Photino",
            _windowTitle,
            _params.Chromeless || _params.FullScreen ? Constants.WS_POPUP : Constants.WS_OVERLAPPEDWINDOW,
            _params.Left, _params.Top, _params.Width, _params.Height,
            IntPtr.Zero,
            IntPtr.Zero,
            _hInstance,
            IntPtr.Zero
        );

        if (_hwnd == IntPtr.Zero)
        {
            uint errorCode = DLLImports.GetLastError();
            Console.WriteLine($"Error creating window. Error Code: {errorCode}");
            return;
        }

        HWNDToPhotino.Add(_hwnd, this);

        if (!string.IsNullOrEmpty(_params.WindowIconFile))
        {
            SetIconFile(_params.WindowIconFile);
        }

        if (_params.CenterOnInitialize)
        {
            Center();
        }

        SetMinimized(_params.Minimized);
        SetMaximized(_params.Maximized);
        SetResizable(_params.Resizable);
        SetTopmost(_params.Topmost);
        SetDarkmodeEnabled(_params.Darkmode);

        // if (initParams->NotificationsEnabled)
        // {
        //     if (_notificationRegistrationId != NULL)
        //         WinToast::instance()->setAppUserModelId(_notificationRegistrationId);
        //
        //     this->_toastHandler = new WinToastHandler(this);
        //     WinToast::instance()->initialize();
        // }

        Show(_params.Minimized || _params.Maximized);
    }

    private IntPtr _hInstance { get; set; }
    private IntPtr _hwnd { get; set; }
    private WinToastHandler? _toastHandler { get; set; }
    public CoreWebView2Environment? WebViewEnvironment { get; private set; }
    public CoreWebView2? WebViewWindow { get; private set; }
    public CoreWebView2Controller? WebViewController { get; private set; }
    private IntPtr darkBrush;
    private IntPtr lightBrush;
    private PhotinoInitParams _params { get; set; }
    private SynchronizationContext _syncContext;


    public void Register()
    {
        _hInstance = DLLImports.GetModuleHandle(null);

        var window = new WNDCLASSEX
        {
            cbSize = (uint) Marshal.SizeOf(typeof(WNDCLASSEX)),
            style = Constants.CS_HREDRAW | Constants.CS_VREDRAW,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(new DLLImports.WndProcDelegate(WindowProc)),
            cbClsExtra = 0,
            cbWndExtra = 0,
            hInstance = _hInstance,
            hbrBackground = GetDarkmodeEnabled() ? darkBrush : lightBrush,
            lpszMenuName = IntPtr.Zero,
            lpszClassName = "Photino"
        };

        var classAtom = DLLImports.RegisterClassEx(ref window);

        if (classAtom == 0)
        {
            var errorCode = DLLImports.GetLastError();
            Console.WriteLine($"Error creating window. Error Code: {errorCode}");
            return;
        }

        DLLImports.SetThreadDpiAwarenessContext(-3);
    }

    private IntPtr WindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case Constants.WM_PAINT:
                {
                    PAINT ps;
                    IntPtr hdc = DLLImports.BeginPaint(hwnd, out ps);

                    if (GetDarkmodeEnabled())
                    {
                        DLLImports.FillRect(hdc, ref ps.rcPaint, darkBrush);
                    }
                    else
                    {
                        DLLImports.FillRect(hdc, ref ps.rcPaint, lightBrush);
                    }

                    DLLImports.EndPaint(hwnd, ref ps);
                    break;
                }
            case Constants.WM_ACTIVATE:
                {
                    if (HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        if (wParam == Constants.WA_INACTIVE)
                        {
                            photino.InvokeFocusOut();
                        }
                        else
                        {
                            photino.FocusWebView2();
                            photino.InvokeFocusIn();

                            return 0;
                        }
                    }

                    break;
                }
            case Constants.WM_CLOSE:
                {
                    if (HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        var doNotClose = photino.InvokeClose();

                        if (!doNotClose)
                        {
                            DLLImports.DestroyWindow(hwnd);
                        }
                    }

                    return 0;
                }
            case Constants.WM_DESTROY:
                {
                    HWNDToPhotino.Remove(hwnd);
                    if (hwnd == messageLoopRootWindowHandle)
                    {
                        DLLImports.PostQuitMessage(0);
                    }

                    return 0;
                }
            case WM_USER_INVOKE:
                {
                    var callbackHandle = GCHandle.FromIntPtr(wParam);
                    var callback = (Action) callbackHandle.Target!;

                    callback?.Invoke();

                    return IntPtr.Zero;
                }
            case Constants.WM_GETMINMAXINFO:
                {
                    if (!HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        return 0;
                    }

                    var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);

                    if (photino.MinWidth > 0)
                    {
                        minMaxInfo.ptMinTrackSize = new POINT()
                        {
                            X = photino.MinWidth,
                            Y = minMaxInfo.ptMinTrackSize.Y
                        };
                    }

                    if (photino.MinHeight > 0)
                    {
                        minMaxInfo.ptMinTrackSize = new POINT()
                        {
                            X = minMaxInfo.ptMinTrackSize.X,
                            Y = photino.MinHeight
                        };
                    }

                    if (photino.MaxWidth < int.MaxValue)
                    {
                        minMaxInfo.ptMaxTrackSize = new POINT()
                        {
                            X = photino.MaxWidth,
                            Y = minMaxInfo.ptMaxTrackSize.Y
                        };
                    }

                    if (photino.MaxHeight < int.MaxValue)
                    {
                        minMaxInfo.ptMaxTrackSize = new POINT()
                        {
                            X = minMaxInfo.ptMaxTrackSize.X,
                            Y = photino.MaxHeight
                        };
                    }

                    Marshal.StructureToPtr(minMaxInfo, lParam, false);

                    return 0;
                }
            case Constants.WM_SIZE:
                {
                    if (HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        photino.RefitContent();

                        var size = photino.GetSize();
                        photino.InvokeResize(size.Width, size.Height);

                        if (wParam == Constants.SIZE_MAXIMIZED)
                        {
                            photino.InvokeMaximized();
                        }
                        else if (wParam == Constants.SIZE_RESTORED)
                        {
                            photino.InvokeRestored();
                        }
                        else if (wParam == Constants.SIZE_MINIMIZED)
                        {
                            photino.InvokeMinimized();
                        }
                    }

                    return 0;
                }
            case Constants.WM_MOVE:
                {
                    if (HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        var point = photino.GetPosition();
                        photino.InvokeMove(point.X, point.Y);
                    }

                    return 0;
                }
            case Constants.WM_MOVING:
                {
                    if (HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        photino.NotifyWebView2WindowMove();
                        photino.RefitContent();
                    }

                    break;
                }
        }

        return DLLImports.DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private const uint WM_USER_INVOKE = (Constants.WM_USER + 0x0002);
    private IntPtr messageLoopRootWindowHandle;
    private Dictionary<IntPtr, WindowsPhotino> HWNDToPhotino = [];
    private string? _webview2RuntimePath;

    private uint RGB(byte r, byte g, byte b)
    {
        return (uint) (r | (g << 8) | (b << 16));
    }

    public void SetWebView2RuntimePath(string? runtimePath)
    {
        if (runtimePath is not null)
        {
            _webview2RuntimePath = runtimePath;
        }
    }

    public IntPtr GetHwnd()
    {
        return _hwnd;
    }

    public void RefitContent()
    {
        if (WebViewController is not null)
        {
            DLLImports.GetClientRect(_hwnd, out var rect);
            WebViewController.Bounds = new Rectangle(new Point(0, 0), new Size(rect.Right - rect.Left, rect.Bottom - rect.Top));
        }
    }

    public void FocusWebView2()
    {
        if (WebViewController is not null)
        {
            WebViewController.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
        }
    }

    public void NotifyWebView2WindowMove()
    {
        if (WebViewController is not null)
        {
            WebViewController.NotifyParentWindowPositionChanged();
        }
    }

    private bool EnsureWebViewIsInstalled()
    {
        var versionInfo = CoreWebView2Environment.GetAvailableBrowserVersionString();

        if (string.IsNullOrWhiteSpace(versionInfo))
        {
            return InstallWebView2();
        }

        return true;
    }

    private bool InstallWebView2()
    {
        var srcUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = srcUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open URL: {ex}");
        }

        return false;
    }

    private async Task AttachWebView()
    {
        var runtimePath = string.IsNullOrWhiteSpace(_webview2RuntimePath) ? _webview2RuntimePath : null;

        // //TODO: Implement special startup strings.
        // //https://peter.sh/experiments/chromium-command-line-switches/
        // //https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environmentoptions.additionalbrowserarguments?view=webview2-dotnet-1.0.1938.49&viewFallbackFrom=webview2-dotnet-1.0.1901.177view%3Dwebview2-1.0.1901.177
        // //https://www.chromium.org/developers/how-tos/run-chromium-with-flags/
        //Add together all 7 special startup strings, plus the generic one passed by the user to make one big string. Try not to duplicate anything. Separate with spaces.


        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(_userAgent))
        {
            sb.Append($"--user-agent=\"{_userAgent}\" ");
        }

        if (_mediaAutoplayEnabled)
        {
            sb.Append("--autoplay-policy=no-user-gesture-required ");
        }

        if (_fileSystemAccessEnabled)
        {
            sb.Append("--allow-file-access-from-files ");
        }

        if (!_webSecurityEnabled)
        {
            sb.Append("--disable-web-security ");
        }

        if (_javascriptClipboardAccessEnabled)
        {
            sb.Append("--enable-javascript-clipboard-access ");
        }

        if (_mediaStreamEnabled)
        {
            sb.Append("--enable-usermedia-screen-capturing ");
        }

        if (!_smoothScrollingEnabled)
        {
            sb.Append("--disable-smooth-scrolling ");
        }

        if (_ignoreCertificateErrorsEnabled)
        {
            sb.Append("--ignore-certificate-errors ");
        }

        if (!string.IsNullOrWhiteSpace(_browserControlInitParameters))
        {
            sb.Append(_browserControlInitParameters); // e.g. --hide-scrollbars
        }

        var options = new CoreWebView2EnvironmentOptions();
        options.AdditionalBrowserArguments = sb.ToString();
        WebViewEnvironment = await CoreWebView2Environment.CreateAsync(runtimePath, _temporaryFilesPath, options);
        WebViewController = await WebViewEnvironment.CreateCoreWebView2ControllerAsync(_hwnd);
        WebViewWindow = WebViewController.CoreWebView2;

        var settings = WebViewWindow.Settings;
        settings.AreHostObjectsAllowed = true;
        settings.IsScriptEnabled = true;
        settings.AreDefaultScriptDialogsEnabled = true;
        settings.IsWebMessageEnabled = true;

        await WebViewWindow.AddScriptToExecuteOnDocumentCreatedAsync(
            "window.external = { sendMessage: function(message) { window.chrome.webview.postMessage(message); }, receiveMessage: function(callback) { window.chrome.webview.addEventListener(\'message\', function(e) { callback(e.data); }); } };");
        WebViewWindow.WebMessageReceived += (_, args) =>
        {
            // Console.WriteLine(args.TryGetWebMessageAsString());
            var message = args.TryGetWebMessageAsString();
            _WebMessageReceivedCallback?.Invoke(message);
        };

        WebViewWindow.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        WebViewWindow.WebResourceRequested += (_, args) =>
        {
            var request = args.Request;

            var uri = request.Uri;
            var colonPos = uri.IndexOf(":", StringComparison.Ordinal);
            if (colonPos > 0)
            {
                var scheme = uri.Substring(0, colonPos);
                if (_customSchemeNames.Contains(scheme))
                {
                    var callback = _customSchemeCallback;

                    var memoryStream = callback!.Invoke(uri, out var contentType);

                    var response = WebViewEnvironment.CreateWebResourceResponse(
                        memoryStream,
                        200,
                        "OK",
                        $"Content-Type: {contentType}");

                    args.Response = response;
                }
            }
        };

        WebViewWindow?.PermissionRequested += (_, args) =>
        {
            if (_grantBrowserPermissions)
            {
                args.State = CoreWebView2PermissionState.Allow;
            }
        };

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
            Console.WriteLine("Neither StartUrl nor StartString was specified");
            Environment.Exit(69);
        }

        if (!ContextMenuEnabled)
        {
            SetContextMenuEnabled(false);
        }

        if (!_devToolsEnabled)
        {
            SetDevToolsEnabled(false);
        }

        if (_transparentEnabled)
        {
            SetTransparentEnabled(true);
        }

        if (_zoom != 100)
        {
            SetZoom(_zoom);
        }

        RefitContent();
        FocusWebView2();
    }

    public void Show(bool isAlreadyShown)
    {
        if (!isAlreadyShown)
        {
            DLLImports.ShowWindow(_hwnd, unchecked((int) Constants.SW_SHOWDEFAULT));
        }

        DLLImports.UpdateWindow(_hwnd);
        // Strangely, it only works to create the webview2 *after* the window has been shown,
        // so defer it until here. This unfortunately means you can't call the Navigate methods
        // until the window is shown.

        if (WebViewController is null)
        {
            if (!string.IsNullOrWhiteSpace(_webview2RuntimePath) || EnsureWebViewIsInstalled())
            {
                var attachTask = AttachWebView();

                while (!attachTask.IsCompleted)
                {
                    if (DLLImports.PeekMessage(out MSG msg, IntPtr.Zero, 0, 0, Constants.PM_REMOVE))
                    {
                        DLLImports.TranslateMessage(ref msg);
                        DLLImports.DispatchMessage(ref msg);
                    }
                }

                attachTask.GetAwaiter().GetResult();
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }

    public void Center()
    {
        var screenDpi = DLLImports.GetDpiForWindow(_hwnd);
        var screenHeight = DLLImports.GetSystemMetricsForDpi(Constants.SM_CYSCREEN, screenDpi);
        var screenWidth = DLLImports.GetSystemMetricsForDpi(Constants.SM_CXSCREEN, screenDpi);

        if (!DLLImports.GetWindowRect(_hwnd, out var rect))
        {
            throw new ApplicationException("Could not get window Rect");
        }

        var left = (screenWidth / 2) - (rect.Width / 2);
        var right = (screenHeight / 2) - (rect.Height / 2);

        SetPosition(left, right);
    }

    public void SetPosition(int x, int y)
    {
        DLLImports.SetWindowPos(_hwnd, 0, x, y, 0, 0, Constants.SWP_NOSIZE | Constants.SWP_NOZORDER);
    }

    public override void ClearBrowserAutoFill()
    {
        // if (!_webviewWindow)
        //     return;
        //
        // auto webview15 = _webviewWindow.try_query<ICoreWebView2_15>();
        // if (webview15)
        // {
        //     wil::com_ptr<ICoreWebView2Profile> profile;
        //     webview15->get_Profile(&profile);
        //     auto profile2 = profile.try_query<ICoreWebView2Profile2>();
        //
        //     if (profile2)
        //     {
        //         COREWEBVIEW2_BROWSING_DATA_KINDS dataKinds =
        //             (COREWEBVIEW2_BROWSING_DATA_KINDS)
        //             (COREWEBVIEW2_BROWSING_DATA_KINDS_GENERAL_AUTOFILL |
        //              COREWEBVIEW2_BROWSING_DATA_KINDS_PASSWORD_AUTOSAVE);
        //
        //         profile2->ClearBrowsingData(
        //             dataKinds,
        //             Callback<ICoreWebView2ClearBrowsingDataCompletedHandler>(
        //                 [this](HRESULT error)
        //                 -> HRESULT {
        //             return S_OK;
        //         })
        //         .Get());
        //     }
        // }
    }

    public override void Close()
    {
        DLLImports.SendMessage(_hwnd, Constants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    public override bool GetDarkmodeEnabled()
    {
        return _darkmodeEnabled;
    }

    public override void SetDarkmodeEnabled(bool darkmode)
    {
        _darkmodeEnabled = darkmode;
        var simple = darkmode ? 1 : 0;
        var result = DLLImports.DwmSetWindowAttribute(_hwnd, Constants.DWMWA_USE_IMMERSIVE_DARK_MODE, ref simple, sizeof(uint));

        if (result != Constants.S_OK)
        {
            DLLImports.DwmSetWindowAttribute(_hwnd, Constants.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref simple, sizeof(uint));
        }
    }

    public override bool GetTransparentEnabled()
    {
        return WebViewController!.DefaultBackgroundColor.A == 0;
    }

    public override bool GetContextMenuEnabled()
    {
        return WebViewWindow!.Settings.AreDefaultContextMenusEnabled;
    }

    public override bool GetDevToolsEnabled()
    {
        return WebViewWindow!.Settings.AreDevToolsEnabled;
    }

    public override bool GetFullScreen()
    {
        var styles = DLLImports.GetWindowLong(_hwnd, Constants.GWL_STYLE);

        if ((styles & Constants.WS_POPUP) != 0)
        {
            return true;
        }

        return false;
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
        var styles = DLLImports.GetWindowLong(_hwnd, Constants.GWL_STYLE);
        if ((styles & Constants.WS_MAXIMIZE) != 0)
        {
            return true;
        }

        return false;
    }

    public override bool GetMinimized()
    {
        var styles = DLLImports.GetWindowLong(_hwnd, Constants.GWL_STYLE);
        if ((styles & Constants.WS_MINIMIZE) != 0)
        {
            return true;
        }

        return false;
    }

    public override Point GetPosition()
    {
        DLLImports.GetWindowRect(_hwnd, out var rect);
        return new Point(rect.Left, rect.Top);
    }

    public override bool GetResizable()
    {
        var styles = DLLImports.GetWindowLong(_hwnd, Constants.GWL_STYLE);
        if ((styles & Constants.WS_THICKFRAME) != 0)
        {
            return true;
        }

        return false;
    }

    public override uint GetScreenDpi()
    {
        return DLLImports.GetDpiForWindow(_hwnd);
    }

    public override Size GetSize()
    {
        DLLImports.GetWindowRect(_hwnd, out var rect);
        return new Size(rect.Width, rect.Height);
    }

    public override string GetTitle()
    {
        return _windowTitle;
    }

    public override bool GetTopmost()
    {
        var styles = DLLImports.GetWindowLong(_hwnd, Constants.GWL_EXSTYLE);
        if ((styles & Constants.WS_EX_TOPMOST) != 0)
        {
            return true;
        }

        return false;
    }

    public override int GetZoom()
    {
        var rawValue = WebViewController!.ZoomFactor;
        rawValue = (rawValue * 100.0) + 0.5;
        return (int) rawValue;
    }

    public override bool GetIgnoreCertificateErrorsEnabled()
    {
        return _ignoreCertificateErrorsEnabled;
    }

    public override void NavigateToString(string content)
    {
        WebViewWindow!.NavigateToString(content);
    }

    public override void NavigateToUrl(string url)
    {
        WebViewWindow!.Navigate(url);
    }

    public override void Restore()
    {
        DLLImports.ShowWindow(_hwnd, unchecked((int) Constants.SW_RESTORE));
    }

    public override void SendWebMessage(string message)
    {
        WebViewWindow!.PostWebMessageAsString(message);
    }

    public override void SetTransparentEnabled(bool enabled)
    {
        var background = WebViewController!.DefaultBackgroundColor;
        WebViewController!.DefaultBackgroundColor = Color.FromArgb(enabled ? 0 : 255, background);
        WebViewWindow!.Reload();
    }

    public override void SetContextMenuEnabled(bool enabled)
    {
        WebViewWindow!.Settings.AreDefaultContextMenusEnabled = enabled;
        WebViewWindow.Reload();
    }

    public override void SetDevToolsEnabled(bool enabled)
    {
        WebViewWindow!.Settings.AreDevToolsEnabled = enabled;
        WebViewWindow.Reload();
    }

    public override void SetIconFile(string filename)
    {
        var iconSmall = DLLImports.LoadImage(_hInstance, filename, Constants.IMAGE_ICON, 16, 16,
            Constants.LR_LOADFROMFILE | Constants.LR_DEFAULTSIZE | Constants.LR_SHARED);
        var iconBig = DLLImports.LoadImage(_hInstance, filename, Constants.IMAGE_ICON, 32, 32,
            Constants.LR_LOADFROMFILE | Constants.LR_DEFAULTSIZE | Constants.LR_SHARED);

        DLLImports.SendMessage(_hwnd, Constants.WM_SETICON, Constants.ICON_BIG, iconBig);
        DLLImports.SendMessage(_hwnd, Constants.WM_SETICON, Constants.ICON_SMALL, iconSmall);

        _iconFileName = filename;
    }

    public override void SetFullScreen(bool fullScreen)
    {
        var style = DLLImports.GetWindowLong(_hwnd, Constants.GWL_STYLE);
        if (fullScreen)
        {
            style |= Constants.WS_POPUP;
            style &= (~Constants.WS_OVERLAPPEDWINDOW);
            SetPosition(0, 0);

            SetSize(
                new Size(
                    DLLImports.GetSystemMetrics(Constants.SM_CXSCREEN),
                    DLLImports.GetSystemMetrics(Constants.SM_CYSCREEN)
                )
            );
        }

        DLLImports.SetWindowLong(_hwnd, Constants.GWL_STYLE, style);
    }

    public override void SetMaximized(bool maximized)
    {
        if (maximized)
        {
            DLLImports.ShowWindow(_hwnd, Constants.SW_MAXIMIZE);
        }
        else
        {
            DLLImports.ShowWindow(_hwnd, Constants.SW_NORMAL);
        }
    }

    public void SetMaxSize(Size size)
    {
        MaxWidth = size.Width;
        MaxHeight = size.Height;

        var currSize = GetSize();

        if (currSize.Width > MaxWidth)
        {
            SetSize(new Size(MaxWidth, currSize.Height));
        }

        if (currSize.Height > MaxHeight)
        {
            SetSize(new Size(currSize.Width, MaxHeight));
        }
    }

    public override void SetMinimized(bool minimized)
    {
        if (minimized)
        {
            DLLImports.ShowWindow(_hwnd, Constants.SW_MINIMIZE);
        }
        else
        {
            DLLImports.ShowWindow(_hwnd, Constants.SW_NORMAL);
        }
    }

    public void SetMinSize(Size size)
    {
        MinWidth = size.Width;
        MinHeight = size.Height;

        var currSize = GetSize();

        if (currSize.Width < MinWidth)
        {
            SetSize(new Size(MinWidth, currSize.Height));
        }

        if (currSize.Height < MinHeight)
        {
            SetSize(new Size(currSize.Width, MinHeight));
        }
    }

    public override void SetPosition(Point position)
    {
        DLLImports.SetWindowPos(
            _hwnd,
            Constants.HWND_TOPMOST,
            position.X,
            position.Y,
            0,
            0,
            Constants.SWP_NOSIZE | Constants.SWP_NOZORDER
        );
    }

    public override void SetResizable(bool resizable)
    {
        var style = DLLImports.GetWindowLongPtr(_hwnd, Constants.GWL_STYLE);
        if (resizable)
        {
            style |= Constants.WS_THICKFRAME | Constants.WS_MINIMIZEBOX | Constants.WS_MAXIMIZEBOX;
        }
        else
        {
            style &= (~Constants.WS_THICKFRAME) & (~Constants.WS_MINIMIZEBOX) & (~Constants.WS_MAXIMIZEBOX);
        }

        DLLImports.SetWindowLong(_hwnd, Constants.GWL_STYLE, style);
    }

    public override void SetSize(Size size)
    {
        DLLImports.SetWindowPos(
            _hwnd,
            Constants.HWND_TOP,
            0,
            0,
            size.Width,
            size.Height,
            Constants.SWP_NOMOVE | Constants.SWP_NOZORDER
        );
    }

    public override void SetTitle(string title)
    {
        DLLImports.SetWindowText(_hwnd, title);

        // if (_notificationsEnabled)
        // {
        //     WinToast::instance()->setAppName(title);
        //     if (_notificationRegistrationId == NULL)
        //         WinToast::instance()->setAppUserModelId(title);
        // }
    }

    public override void SetTopmost(bool topmost)
    {
        var style = DLLImports.GetWindowLongPtr(_hwnd, Constants.GWL_EXSTYLE);
        if (topmost)
        {
            style |= Constants.WS_EX_TOPMOST;
        }
        else
        {
            style &= (~Constants.WS_EX_TOPMOST);
        }

        DLLImports.SetWindowLong(_hwnd, Constants.GWL_EXSTYLE, style);
        DLLImports.SetWindowPos(_hwnd,
            topmost ? Constants.HWND_TOPMOST : Constants.HWND_NOTOPMOST,
            0,
            0,
            0,
            0,
            Constants.SWP_NOMOVE | Constants.SWP_NOSIZE
        );
    }

    public override void SetZoom(int zoom)
    {
        WebViewController!.ZoomFactor = zoom / 100.0;
    }

    public override void ShowNotification(string title, string message)
    {
        // title = ToUTF16String(title);
        // body = ToUTF16String(body);
        // if (_notificationsEnabled && WinToast::isCompatible())
        // {
        //     WinToastTemplate toast = WinToastTemplate(WinToastTemplate::ImageAndText02);
        //     toast.setTextField(title, WinToastTemplate::FirstLine);
        //     toast.setTextField(body, WinToastTemplate::SecondLine);
        //     if (this->_iconFileName != NULL)
        //         toast.setImagePath(this->_iconFileName);
        //     WinToast::instance()->showToast(toast, _toastHandler);
        // }
    }

    public override void WaitForExit()
    {
        messageLoopRootWindowHandle = _hwnd;

        while (DLLImports.GetMessage(out var msg, IntPtr.Zero, 0, 0))
        {
            DLLImports.TranslateMessage(ref msg);
            DLLImports.DispatchMessage(ref msg);
        }
    }

    public override void AddCustomSchemeName(string scheme)
    {
        _customSchemeNames.Add(scheme);
    }

    public override List<Monitor> GetAllMonitors()
    {
        // if (callback)
        // {
        //     EnumDisplayMonitors(NULL, NULL, (MONITORENUMPROC) MonitorEnum, (LPARAM)callback);
        // }
        return new List<Monitor>();
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

    public List<string> GetResults(IFileOpenDialog dialog, bool multiSelect)
    {
        var result = new List<string>();

        if (multiSelect)
        {
            dialog.GetResults(out IShellItemArray results);
            results.GetCount(out uint count);

            for (uint i = 0; i < count; i++)
            {
                results.GetItemAt(i, out IShellItem item);
                item.GetDisplayName(SIGDN.FILESYSPATH, out string pathToUse);
                result.Add(pathToUse);
            }

            return result;
        }
        else
        {
            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(SIGDN.FILESYSPATH, out string pathToUse);
            result.Add(pathToUse);
            return result;
        }
    }

    public override async Task<List<string>> ShowOpenFileAsync(string title, string? path, bool multiSelect,
        List<FileFilter>? filterPatterns)
    {
        var dialog = (IFileOpenDialog) Activator.CreateInstance(Type.GetTypeFromCLSID(Constants.CLSID_FileOpenDialog));
        var result = new List<string>();

        try
        {
            dialog!.GetOptions(out uint options);
            options |= Constants.FOS_FILEMUSTEXIST | Constants.FOS_FORCEFILESYSTEM | Constants.FOS_PATHMUSTEXIST;
            if (multiSelect)
            {
                options |= Constants.FOS_ALLOWMULTISELECT;
            }
            dialog.SetOptions(options);
            dialog.SetTitle(title);
            dialog.SetOkButtonLabel("Select");

            filterPatterns ??= new List<FileFilter>()
            {
                new FileFilter("All Files", "*.*")
            };

            var specs = filterPatterns.Select(f => new COMDLG_FILTERSPEC()
            {
                pszName = f.Name,
                pszSpec = f.Spec
            }).ToArray();

            dialog.SetFileTypes((uint) specs.Length, specs);
            dialog.SetFileTypeIndex(1);

            if (!string.IsNullOrEmpty(path))
            {
                var iid = typeof(IShellItem).GUID;
                if (DLLImports.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out IShellItem folder) == Constants.S_OK)
                {
                    dialog.SetFolder(folder);
                }
            }

            var hr = dialog.Show(_hwnd);

            if (hr == Constants.ERROR_CANCELLED)
            {
                return result;
            }

            if (hr != Constants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return GetResults(dialog, multiSelect);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    public override async Task<List<string>> ShowOpenFolderAsync(string title, string? path, bool multiSelect)
    {
        var dialog = (IFileOpenDialog) Activator.CreateInstance(Type.GetTypeFromCLSID(Constants.CLSID_FileOpenDialog));
        var result = new List<string>();

        try
        {
            dialog!.GetOptions(out uint options);
            options |= Constants.FOS_PICKFOLDERS | Constants.FOS_FORCEFILESYSTEM | Constants.FOS_PATHMUSTEXIST;
            if (multiSelect)
            {
                options |= Constants.FOS_ALLOWMULTISELECT;
            }
            dialog.SetOptions(options);
            dialog.SetTitle(title);
            dialog.SetOkButtonLabel("Select");

            if (!string.IsNullOrEmpty(path))
            {
                var iid = typeof(IShellItem).GUID;
                if (DLLImports.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out IShellItem folder) == Constants.S_OK)
                {
                    dialog.SetFolder(folder);
                }
            }

            var hr = dialog.Show(_hwnd);

            if (hr == Constants.ERROR_CANCELLED)
            {
                return result;
            }

            if (hr != Constants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return GetResults(dialog, multiSelect);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    public override async Task<string> ShowSaveFileAsync(string title, string? path, List<FileFilter>? filterPatterns,
        string defaultExtension = "txt", string defaultFileName = "PhotinoExFile")
    {
        var dialog = (IFileSaveDialog) Activator.CreateInstance(Type.GetTypeFromCLSID(Constants.CLSID_FileSaveDialog));

        try
        {
            dialog!.GetOptions(out uint options);
            options |= Constants.FOS_FORCEFILESYSTEM | Constants.FOS_PATHMUSTEXIST | Constants.FOS_OVERWRITEPROMPT;
            dialog.SetOptions(options);

            dialog.SetTitle(title);
            dialog.SetOkButtonLabel("Save");

            filterPatterns ??= new List<FileFilter>();
            if (!filterPatterns.Any())
            {
                filterPatterns.Add(new FileFilter("All Files", "*.*"));
            }

            var specs = filterPatterns.Select(f => new COMDLG_FILTERSPEC
            {
                pszName = f.Name,
                pszSpec = f.Spec
            }).ToArray();

            dialog.SetFileTypes((uint) specs.Length, specs);
            dialog.SetFileTypeIndex(1);

            if (!string.IsNullOrEmpty(defaultFileName))
            {
                dialog.SetFileName(defaultFileName);
            }

            if (!string.IsNullOrEmpty(defaultExtension))
            {
                dialog.SetDefaultExtension(defaultExtension.TrimStart('.'));
            }

            if (!string.IsNullOrEmpty(path))
            {
                var iid = typeof(IShellItem).GUID;
                if (DLLImports.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out IShellItem startFolder) == Constants.S_OK)
                {
                    dialog.SetFolder(startFolder);
                }
            }

            int hr = dialog.Show(_hwnd);

            if (hr == Constants.ERROR_CANCELLED)
            {
                return "";
            }

            if (hr != Constants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(SIGDN.FILESYSPATH, out string pathToUse);
            return pathToUse;
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    public override async Task<DialogResult> ShowMessageAsync(string title, string text, DialogButtons buttons, DialogIcon icon)
    {
        uint flags = 0;

        switch (icon)
        {
            case DialogIcon.Info:
                flags |= Constants.MB_ICONINFORMATION;
                break;
            case DialogIcon.Warning:
                flags |= Constants.MB_ICONWARNING;
                break;
            case DialogIcon.Error:
                flags |= Constants.MB_ICONERROR;
                break;
            case DialogIcon.Question:
                flags |= Constants.MB_ICONQUESTION;
                break;
        }

        switch (buttons)
        {
            case DialogButtons.Ok:
                flags |= Constants.MB_OK;
                break;
            case DialogButtons.OkCancel:
                flags |= Constants.MB_OKCANCEL;
                break;
            case DialogButtons.YesNo:
                flags |= Constants.MB_YESNO;
                break;
            case DialogButtons.YesNoCancel:
                flags |= Constants.MB_YESNOCANCEL;
                break;
            case DialogButtons.RetryCancel:
                flags |= Constants.MB_RETRYCANCEL;
                break;
            case DialogButtons.AbortRetryIgnore:
                flags |= Constants.MB_ABORTRETRYIGNORE;
                break;
        }

        int result = DLLImports.MessageBoxW(_hwnd, text, title, flags);

        switch (result)
        {
            case Constants.IDOK:
                return DialogResult.Ok;
            case Constants.IDCANCEL:
                return DialogResult.Cancel;
            case Constants.IDYES:
                return DialogResult.Yes;
            case Constants.IDNO:
                return DialogResult.No;
            case Constants.IDABORT:
                return DialogResult.Abort;
            case Constants.IDRETRY:
                return DialogResult.Retry;
            case Constants.IDIGNORE:
                return DialogResult.Ignore;
            default:
                return DialogResult.Cancel;
        }
    }

    public override void Invoke(Action callback)
    {
        var callbackHandle = GCHandle.Alloc(callback);

        try
        {
            DLLImports.SendMessage(_hwnd, WM_USER_INVOKE, GCHandle.ToIntPtr(callbackHandle), IntPtr.Zero);
        }
        finally
        {
            callbackHandle.Free();
        }
    }
}
