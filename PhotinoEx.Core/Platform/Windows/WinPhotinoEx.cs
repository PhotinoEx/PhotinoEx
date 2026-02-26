using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using PhotinoEx.Core.Models;
using PhotinoEx.Core.Platform.Windows.Dialog;
using PhotinoEx.Core.Utils;
using Monitor = PhotinoEx.Core.Models.Monitor;
using Size = System.Drawing.Size;

namespace PhotinoEx.Core.Platform.Windows;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
[SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
public class WinPhotinoEx : PhotinoEx
{
    private IntPtr _hInstance { get; set; }
    private IntPtr _hwnd { get; set; }
    public CoreWebView2Environment? WebViewEnvironment { get; private set; }
    public CoreWebView2? WebViewWindow { get; private set; }
    public CoreWebView2Controller? WebViewController { get; private set; }
    private IntPtr darkBrush { get; set; }
    private IntPtr lightBrush { get; set; }
    private PhotinoExInitParams _params { get; set; }
    private SynchronizationContext _syncContext { get; set; }
    private bool _windowsThemeIsDark { get; set; }
    private const uint WM_USER_INVOKE = (WinConstants.WM_USER + 0x0002);
    private IntPtr messageLoopRootWindowHandle;
    private Dictionary<IntPtr, WinPhotinoEx> HWNDToPhotino = [];
    private string? _webview2RuntimePath;

    public WinPhotinoEx(PhotinoExInitParams exInitParams)
    {
        darkBrush = WinAPi.CreateSolidBrush(RGB(0, 0, 0));
        lightBrush = WinAPi.CreateSolidBrush(RGB(255, 255, 255));

        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _params = exInitParams;

        _windowTitle = string.IsNullOrEmpty(_params.Title) ? "Set a title" : _params.Title;
        // they also did wintoast things here
        _startUrl = _params.StartUrl;
        _startString = _params.StartString;
        _temporaryFilesPath = _params.TemporaryFilesPath;
        _userAgent = _params.UserAgent;
        _browserControlInitParameters = _params.BrowserControlInitParameters;
        _notificationRegistrationId = _params.NotificationRegistrationId;

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
            _params.Width = unchecked((int) WinConstants.CW_USEDEFAULT);
            _params.Height = unchecked((int) WinConstants.CW_USEDEFAULT);
        }
        else
        {
            if (_params.Width < 0)
            {
                _params.Width = unchecked((int) WinConstants.CW_USEDEFAULT);
            }

            if (_params.Height < 0)
            {
                _params.Height = unchecked((int) WinConstants.CW_USEDEFAULT);
            }
        }

        if (_params.UseOsDefaultLocation)
        {
            _params.Left = unchecked((int) WinConstants.CW_USEDEFAULT);
            _params.Top = unchecked((int) WinConstants.CW_USEDEFAULT);
        }

        if (_params.FullScreen)
        {
            _params.Left = 0;
            _params.Top = 0;
            _params.Width = WinAPi.GetSystemMetrics(WinConstants.SM_CXSCREEN);
            _params.Height = WinAPi.GetSystemMetrics(WinConstants.SM_CYSCREEN);
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

        _hwnd = WinAPi.CreateWindowEx(
            _params.Transparent ? WinConstants.WS_EX_LAYERED : 0,
            "Photino",
            _windowTitle,
            _params.Chromeless || _params.FullScreen ? WinConstants.WS_POPUP : WinConstants.WS_OVERLAPPEDWINDOW,
            _params.Left, _params.Top, _params.Width, _params.Height,
            IntPtr.Zero,
            IntPtr.Zero,
            _hInstance,
            IntPtr.Zero
        );

        Dialog = new WinPhotinoExDialog(_hwnd);
        SetAppTheme(_windowsThemeIsDark);
        SetBackdropTheme();

        if (_hwnd == IntPtr.Zero)
        {
            uint errorCode = WinAPi.GetLastError();
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

    private void SetBackdropTheme()
    {
        var backdrop = (int) WindowBackdropType.Mica;
        WinAPi.DwmSetWindowAttribute(_hwnd, WinConstants.DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(WindowBackdropType));
    }

    public void Register()
    {
        _hInstance = WinAPi.GetModuleHandle(null);
        _windowsThemeIsDark = CheckWindowsThemeIsDark();

        var window = new WndClassEx()
        {
            cbSize = (uint) Marshal.SizeOf(typeof(WndClassEx)),
            style = WinConstants.CS_HREDRAW | WinConstants.CS_VREDRAW,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(new WinAPi.WndProcDelegate(WindowProc)),
            cbClsExtra = 0,
            cbWndExtra = 0,
            hInstance = _hInstance,
            hbrBackground = _windowsThemeIsDark ? darkBrush : lightBrush,
            lpszMenuName = IntPtr.Zero,
            lpszClassName = "Photino"
        };

        var classAtom = WinAPi.RegisterClassEx(ref window);

        if (classAtom == 0)
        {
            var errorCode = WinAPi.GetLastError();
            Console.WriteLine($"Error creating window. Error Code: {errorCode}");
            return;
        }

        WinAPi.SetThreadDpiAwarenessContext(-3);
    }

    private IntPtr WindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WinConstants.WM_PAINT:
                {
                    Paint ps;
                    IntPtr hdc = WinAPi.BeginPaint(hwnd, out ps);

                    if (_windowsThemeIsDark)
                    {
                        WinAPi.FillRect(hdc, ref ps.rcPaint, darkBrush);
                    }
                    else
                    {
                        WinAPi.FillRect(hdc, ref ps.rcPaint, lightBrush);
                    }

                    WinAPi.EndPaint(hwnd, ref ps);
                    break;
                }
            case WinConstants.WM_SETTINGCHANGE:
                {
                    var param = Marshal.PtrToStringAnsi(lParam);
                    if (param == "ImmersiveColorSet")
                    {
                        _windowsThemeIsDark = CheckWindowsThemeIsDark();
                        SetAppTheme(_windowsThemeIsDark);
                    }

                    break;
                }
            case WinConstants.WM_ACTIVATE:
                {
                    if (HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        if (wParam == WinConstants.WA_INACTIVE)
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
            case WinConstants.WM_CLOSE:
                {
                    if (HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        var doNotClose = photino.InvokeClose();

                        if (!doNotClose)
                        {
                            WinAPi.DestroyWindow(hwnd);
                        }
                    }

                    return 0;
                }
            case WinConstants.WM_DESTROY:
                {
                    HWNDToPhotino.Remove(hwnd);
                    if (hwnd == messageLoopRootWindowHandle)
                    {
                        WinAPi.PostQuitMessage(0);
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
            case WinConstants.WM_GETMINMAXINFO:
                {
                    if (!HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        return 0;
                    }

                    var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);

                    if (photino.MinWidth > 0)
                    {
                        minMaxInfo.ptMinTrackSize = new Point()
                        {
                            X = photino.MinWidth,
                            Y = minMaxInfo.ptMinTrackSize.Y
                        };
                    }

                    if (photino.MinHeight > 0)
                    {
                        minMaxInfo.ptMinTrackSize = new Point()
                        {
                            X = minMaxInfo.ptMinTrackSize.X,
                            Y = photino.MinHeight
                        };
                    }

                    if (photino.MaxWidth < int.MaxValue)
                    {
                        minMaxInfo.ptMaxTrackSize = new Point()
                        {
                            X = photino.MaxWidth,
                            Y = minMaxInfo.ptMaxTrackSize.Y
                        };
                    }

                    if (photino.MaxHeight < int.MaxValue)
                    {
                        minMaxInfo.ptMaxTrackSize = new Point()
                        {
                            X = minMaxInfo.ptMaxTrackSize.X,
                            Y = photino.MaxHeight
                        };
                    }

                    Marshal.StructureToPtr(minMaxInfo, lParam, false);

                    return 0;
                }
            case WinConstants.WM_SIZE:
                {
                    if (HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        photino.RefitContent();

                        var size = photino.GetSize();
                        photino.InvokeResize(size.Width, size.Height);

                        if (wParam == WinConstants.SIZE_MAXIMIZED)
                        {
                            photino.InvokeMaximized();
                        }
                        else if (wParam == WinConstants.SIZE_RESTORED)
                        {
                            photino.InvokeRestored();
                        }
                        else if (wParam == WinConstants.SIZE_MINIMIZED)
                        {
                            photino.InvokeMinimized();
                        }
                    }

                    return 0;
                }
            case WinConstants.WM_MOVE:
                {
                    if (HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        var point = photino.GetPosition();
                        photino.InvokeMove(point.X, point.Y);
                    }

                    return 0;
                }
            case WinConstants.WM_MOVING:
                {
                    if (HWNDToPhotino.TryGetValue(hwnd, out var photino))
                    {
                        photino.NotifyWebView2WindowMove();
                        photino.RefitContent();
                    }

                    break;
                }
        }

        return WinAPi.DefWindowProc(hwnd, msg, wParam, lParam);
    }

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
            WinAPi.GetClientRect(_hwnd, out var rect);
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
            WinAPi.ShowWindow(_hwnd, unchecked((int) WinConstants.SW_SHOWDEFAULT));
        }

        WinAPi.UpdateWindow(_hwnd);
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
                    if (WinAPi.PeekMessage(out Msg msg, IntPtr.Zero, 0, 0, WinConstants.PM_REMOVE))
                    {
                        WinAPi.TranslateMessage(ref msg);
                        WinAPi.DispatchMessage(ref msg);
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
        var screenDpi = WinAPi.GetDpiForWindow(_hwnd);
        var screenHeight = WinAPi.GetSystemMetricsForDpi(WinConstants.SM_CYSCREEN, screenDpi);
        var screenWidth = WinAPi.GetSystemMetricsForDpi(WinConstants.SM_CXSCREEN, screenDpi);

        if (!WinAPi.GetWindowRect(_hwnd, out var rect))
        {
            throw new ApplicationException("Could not get window Rect");
        }

        var left = (screenWidth / 2) - (rect.Width / 2);
        var right = (screenHeight / 2) - (rect.Height / 2);

        SetPosition(left, right);
    }

    public void SetPosition(int x, int y)
    {
        WinAPi.SetWindowPos(_hwnd, 0, x, y, 0, 0, WinConstants.SWP_NOSIZE | WinConstants.SWP_NOZORDER);
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
        WinAPi.SendMessage(_hwnd, WinConstants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    private bool CheckWindowsThemeIsDark()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(WinConstants.WindowsTheme);
        if (key?.GetValue("AppsUseLightTheme") is int value)
        {
            // because its checking if appsUseLightTheme, if it returns 1 its "true"
            return value == 0;
        }

        return false;
    }

    public void SetAppTheme(bool darkmode)
    {
        var simple = darkmode ? 1 : 0;
        // Dark mode is 1 and light mode is 0

        var result = WinAPi.DwmSetWindowAttribute(_hwnd, WinConstants.DWMWA_USE_IMMERSIVE_DARK_MODE, ref simple, sizeof(uint));

        if (result != WinConstants.S_OK)
        {
            WinAPi.DwmSetWindowAttribute(_hwnd, WinConstants.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref simple, sizeof(uint));
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
        var styles = WinAPi.GetWindowLong(_hwnd, WinConstants.GWL_STYLE);

        if ((styles & WinConstants.WS_POPUP) != 0)
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
        var styles = WinAPi.GetWindowLong(_hwnd, WinConstants.GWL_STYLE);
        if ((styles & WinConstants.WS_MAXIMIZE) != 0)
        {
            return true;
        }

        return false;
    }

    public override bool GetMinimized()
    {
        var styles = WinAPi.GetWindowLong(_hwnd, WinConstants.GWL_STYLE);
        if ((styles & WinConstants.WS_MINIMIZE) != 0)
        {
            return true;
        }

        return false;
    }

    public override Point GetPosition()
    {
        WinAPi.GetWindowRect(_hwnd, out var rect);
        return new Point(rect.Left, rect.Top);
    }

    public override bool GetResizable()
    {
        var styles = WinAPi.GetWindowLong(_hwnd, WinConstants.GWL_STYLE);
        if ((styles & WinConstants.WS_THICKFRAME) != 0)
        {
            return true;
        }

        return false;
    }

    public override uint GetScreenDpi()
    {
        return WinAPi.GetDpiForWindow(_hwnd);
    }

    public override Size GetSize()
    {
        WinAPi.GetWindowRect(_hwnd, out var rect);
        return new Size(rect.Width, rect.Height);
    }

    public override string GetTitle()
    {
        return _windowTitle;
    }

    public override bool GetTopmost()
    {
        var styles = WinAPi.GetWindowLong(_hwnd, WinConstants.GWL_EXSTYLE);
        if ((styles & WinConstants.WS_EX_TOPMOST) != 0)
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
        WinAPi.ShowWindow(_hwnd, unchecked((int) WinConstants.SW_RESTORE));
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
        var iconSmall = WinAPi.LoadImage(_hInstance, filename, WinConstants.IMAGE_ICON, 16, 16,
            WinConstants.LR_LOADFROMFILE | WinConstants.LR_DEFAULTSIZE | WinConstants.LR_SHARED);
        var iconBig = WinAPi.LoadImage(_hInstance, filename, WinConstants.IMAGE_ICON, 32, 32,
            WinConstants.LR_LOADFROMFILE | WinConstants.LR_DEFAULTSIZE | WinConstants.LR_SHARED);

        WinAPi.SendMessage(_hwnd, WinConstants.WM_SETICON, WinConstants.ICON_BIG, iconBig);
        WinAPi.SendMessage(_hwnd, WinConstants.WM_SETICON, WinConstants.ICON_SMALL, iconSmall);

        _iconFileName = filename;
    }

    public override void SetFullScreen(bool fullScreen)
    {
        var style = WinAPi.GetWindowLong(_hwnd, WinConstants.GWL_STYLE);
        if (fullScreen)
        {
            style |= WinConstants.WS_POPUP;
            style &= (~WinConstants.WS_OVERLAPPEDWINDOW);
            SetPosition(0, 0);

            SetSize(
                new Size(
                    WinAPi.GetSystemMetrics(WinConstants.SM_CXSCREEN),
                    WinAPi.GetSystemMetrics(WinConstants.SM_CYSCREEN)
                )
            );
        }

        WinAPi.SetWindowLong(_hwnd, WinConstants.GWL_STYLE, style);
    }

    public override void SetMaximized(bool maximized)
    {
        if (maximized)
        {
            WinAPi.ShowWindow(_hwnd, WinConstants.SW_MAXIMIZE);
        }
        else
        {
            WinAPi.ShowWindow(_hwnd, WinConstants.SW_NORMAL);
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
            WinAPi.ShowWindow(_hwnd, WinConstants.SW_MINIMIZE);
        }
        else
        {
            WinAPi.ShowWindow(_hwnd, WinConstants.SW_NORMAL);
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
        WinAPi.SetWindowPos(
            _hwnd,
            WinConstants.HWND_TOPMOST,
            position.X,
            position.Y,
            0,
            0,
            WinConstants.SWP_NOSIZE | WinConstants.SWP_NOZORDER
        );
    }

    public override void SetResizable(bool resizable)
    {
        var style = WinAPi.GetWindowLongPtr(_hwnd, WinConstants.GWL_STYLE);
        if (resizable)
        {
            style |= WinConstants.WS_THICKFRAME | WinConstants.WS_MINIMIZEBOX | WinConstants.WS_MAXIMIZEBOX;
        }
        else
        {
            style &= (~WinConstants.WS_THICKFRAME) & (~WinConstants.WS_MINIMIZEBOX) & (~WinConstants.WS_MAXIMIZEBOX);
        }

        WinAPi.SetWindowLong(_hwnd, WinConstants.GWL_STYLE, style);
    }

    public override void SetSize(Size size)
    {
        WinAPi.SetWindowPos(
            _hwnd,
            WinConstants.HWND_TOP,
            0,
            0,
            size.Width,
            size.Height,
            WinConstants.SWP_NOMOVE | WinConstants.SWP_NOZORDER
        );
    }

    public override void SetTitle(string title)
    {
        WinAPi.SetWindowText(_hwnd, title);

        // if (_notificationsEnabled)
        // {
        //     WinToast::instance()->setAppName(title);
        //     if (_notificationRegistrationId == NULL)
        //         WinToast::instance()->setAppUserModelId(title);
        // }
    }

    public override void SetTopmost(bool topmost)
    {
        var style = WinAPi.GetWindowLongPtr(_hwnd, WinConstants.GWL_EXSTYLE);
        if (topmost)
        {
            style |= WinConstants.WS_EX_TOPMOST;
        }
        else
        {
            style &= (~WinConstants.WS_EX_TOPMOST);
        }

        WinAPi.SetWindowLong(_hwnd, WinConstants.GWL_EXSTYLE, style);
        WinAPi.SetWindowPos(_hwnd,
            topmost ? WinConstants.HWND_TOPMOST : WinConstants.HWND_NOTOPMOST,
            0,
            0,
            0,
            0,
            WinConstants.SWP_NOMOVE | WinConstants.SWP_NOSIZE
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

        while (WinAPi.GetMessage(out var msg, IntPtr.Zero, 0, 0))
        {
            WinAPi.TranslateMessage(ref msg);
            WinAPi.DispatchMessage(ref msg);
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

    public override void Invoke(Action callback)
    {
        var callbackHandle = GCHandle.Alloc(callback);

        try
        {
            WinAPi.SendMessage(_hwnd, WM_USER_INVOKE, GCHandle.ToIntPtr(callbackHandle), IntPtr.Zero);
        }
        finally
        {
            callbackHandle.Free();
        }
    }
}
