using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.Raw;
using PhotinoEx.Core.Enums;
using PhotinoEx.Core.TempModels;
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
            initParams.Left = 0;
            initParams.Top = 0;
            initParams.Width = DLLImports.GetSystemMetrics(Constants.SM_CXSCREEN);
            initParams.Height = DLLImports.GetSystemMetrics(Constants.SM_CYSCREEN);
        }

        if (initParams.Chromeless)
        {
            //CW_USEDEFAULT CAN NOT BE USED ON POPUP WINDOWS
            if (initParams.Left == Constants.CW_USEDEFAULT && initParams.Top == Constants.CW_USEDEFAULT)
            {
                initParams.CenterOnInitialize = true;
            }

            if (initParams.Left == Constants.CW_USEDEFAULT)
            {
                initParams.Left = 0;
            }

            if (initParams.Top == Constants.CW_USEDEFAULT)
            {
                initParams.Top = 0;
            }

            if (initParams.Height == Constants.CW_USEDEFAULT)
            {
                initParams.Height = 600;
            }

            if (initParams.Width == Constants.CW_USEDEFAULT)
            {
                initParams.Width = 800;
            }
        }

        if (initParams.Height > initParams.MaxHeight)
        {
            initParams.Height = initParams.MaxHeight;
        }

        if (initParams.Height < initParams.MinHeight && initParams.MinHeight > 0)
        {
            initParams.Height = initParams.MinHeight;
        }

        if (initParams.Width > initParams.MaxWidth)
        {
            initParams.Width = initParams.MaxWidth;
        }

        if (initParams.Width < initParams.MinWidth && initParams.MinWidth > 0)
        {
            initParams.Width = initParams.MinWidth;
        }

        Register();

        _hwnd = DLLImports.CreateWindowEx(
            initParams.Transparent ? Constants.WS_EX_LAYERED : 0,
            "Photino",
            _windowTitle,
            initParams.Chromeless || initParams.FullScreen ? Constants.WS_POPUP : Constants.WS_OVERLAPPEDWINDOW,
            initParams.Left, initParams.Top, initParams.Width, initParams.Height,
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

        if (string.IsNullOrEmpty(initParams.WindowIconFile))
        {
            SetIconFile(initParams.WindowIconFile);
        }

        if (initParams.CenterOnInitialize)
        {
            Center();
        }

        SetMinimized(initParams.Minimized);
        SetMaximized(initParams.Maximized);
        SetResizable(initParams.Resizable);
        SetTopmost(initParams.Topmost);

        // if (initParams->NotificationsEnabled)
        // {
        //     if (_notificationRegistrationId != NULL)
        //         WinToast::instance()->setAppUserModelId(_notificationRegistrationId);
        //
        //     this->_toastHandler = new WinToastHandler(this);
        //     WinToast::instance()->initialize();
        // }

        Show(initParams.Minimized || initParams.Maximized);
    }

    private IntPtr _hInstance { get; set; }
    private IntPtr _hwnd { get; set; }
    private WinToastHandler? _toastHandler { get; set; }
    private CoreWebView2Environment? _webViewEnvironment { get; set; }
    private CoreWebView2? _webViewWindow { get; set; }
    private CoreWebView2Controller? _webViewController { get; set; }
    private IntPtr darkBrush;
    private IntPtr lightBrush;
    private PhotinoInitParams _params { get; set; }
    private SynchronizationContext _syncContext;

    public void Register()
    {
        _hInstance = DLLImports.GetModuleHandle(null);

        var window = new WNDCLASSEX();
        window.cbSize = (uint) Marshal.SizeOf(typeof(WNDCLASSEX));
        window.style = Constants.CS_HREDRAW | Constants.CS_VREDRAW;
        window.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(new DLLImports.WndProcDelegate(WindowProc));
        window.cbClsExtra = 0;
        window.cbWndExtra = 0;
        window.hInstance = _hInstance;
        window.hbrBackground = IsDarkModeEnabled() ? darkBrush : lightBrush;
        window.lpszMenuName = IntPtr.Zero;
        window.lpszClassName = "Photino";

        var classAtom = DLLImports.RegisterClassEx(ref window);

        if (classAtom == 0)
        {
            uint errorCode = DLLImports.GetLastError();
            Console.WriteLine($"Error creating window. Error Code: {errorCode}");
            return;
        }

        DLLImports.SetThreadDpiAwarenessContext(-3);
    }

    private IntPtr WindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case Constants.WM_CREATE:
                {
                    EnableDarkMode(hwnd, true);
                    if (IsDarkModeEnabled())
                    {
                        RefreshNonClientArea(hwnd);
                    }

                    break;
                }
            case Constants.WM_SETTINGCHANGE:
                {
                    if (IsColorSchemeChange(lParam))
                    {
                        DLLImports.SendMessage(hwnd, Constants.WM_THEMECHANGED, IntPtr.Zero, IntPtr.Zero);
                    }

                    break;
                }
            case Constants.WM_THEMECHANGED:
                {
                    EnableDarkMode(hwnd, IsDarkModeEnabled());
                    RefreshNonClientArea(hwnd);
                    DLLImports.InvalidateRect(hwnd, IntPtr.Zero, true);
                    break;
                }
            case Constants.WM_PAINT:
                {
                    PAINT ps;
                    IntPtr hdc = DLLImports.BeginPaint(hwnd, out ps);

                    if (IsDarkModeEnabled())
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
                    var callback = (Action)callbackHandle.Target;

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

                    if (photino.MinWidth > 0 || photino.MinHeight > 0)
                    {
                        minMaxInfo.ptMinTrackSize = new POINT
                        {
                            X = photino.MinWidth > 0 ? photino.MinWidth : minMaxInfo.ptMinTrackSize.X,
                            Y = photino.MinHeight > 0 ? photino.MinHeight : minMaxInfo.ptMinTrackSize.Y
                        };
                    }

                    if (photino.MaxWidth < int.MaxValue || photino.MaxHeight < int.MaxValue)
                    {
                        minMaxInfo.ptMaxTrackSize = new POINT
                        {
                            X = photino.MaxWidth < int.MaxValue ? photino.MaxWidth : minMaxInfo.ptMaxTrackSize.X,
                            Y = photino.MaxHeight < int.MaxValue ? photino.MaxHeight : minMaxInfo.ptMaxTrackSize.Y
                        };
                    }

                    Marshal.StructureToPtr(minMaxInfo, lParam, true);

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
                        NotifyWebView2WindowMove();
                        RefitContent();
                    }

                    break;
                }
        }

        return DLLImports.DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private const uint WM_USER_INVOKE = (Constants.WM_USER + 0x0002);
    private IntPtr messageLoopRootWindowHandle;
    private Dictionary<IntPtr, WindowsPhotino> HWNDToPhotino = [];

    private bool IsColorSchemeChange(IntPtr lParam)
    {
        var result = false;
        if (lParam > 0)
        {
            var lparam = Marshal.PtrToStructure<string>(lParam);
            if (string.Equals(lparam, "ImmersiveColorSet", StringComparison.OrdinalIgnoreCase))
            {
                // RefreshImmersiveColorPolicyState();
                result = true;
            }
        }

        // GetIsImmersiveColorUsingHighContrast(IHCM_REFRESH);
        return result;
    }

    private void RefreshNonClientArea(IntPtr hwnd)
    {
        // if (IsDarkModeAllowedForWindow == IntPtr.Zero || ShouldAppsUseDarkMode == IntPtr.Zero)
        // {
        //     return;
        // }
        //
        // var dark = false;
        // if (IsDarkModeAllowedForWindow(hwnd) && ShouldAppsUseDarkMode() && !IsHighContrast())
        // {
        //     dark = true;
        // }
        //
        // if (SetWindowCompositionAttribute != IntPtr.Zero)
        // {
        //     var data
        // }

        // if (IsDarkModeAllowedForWindow == nullptr ||
        //     ShouldAppsUseDarkMode == nullptr) {
        //     return;
        // }
        //
        // BOOL dark = FALSE;
        // if (IsDarkModeAllowedForWindow(hwnd) == TRUE &&
        //     ShouldAppsUseDarkMode() == TRUE && !IsHighContrast()) {
        //     dark = TRUE;
        // }
        //
        // if (SetWindowCompositionAttribute != nullptr) {
        //     WINDOWCOMPOSITIONATTRIBDATA data = {
        //         WCA_USEDARKMODECOLORS, &dark, sizeof(dark)};
        //     SetWindowCompositionAttribute(hwnd, &data);
        // }
    }

    private uint RGB(byte r, byte g, byte b)
    {
        return (uint) (r | (g << 8) | (b << 16));
    }

    private bool IsDarkModeEnabled()
    {
        // if (ShouldAppsUserDarkMode == IntPtr.Zero)
        // {
        //     return false;
        // }
        //
        // return ShouldAppsUserDarkMode && !IsHighContrast();

        // if (ShouldAppsUseDarkMode == nullptr) {
        //     return false;
        // }
        // return (ShouldAppsUseDarkMode() == TRUE) && !IsHighContrast();
        return true;
    }

    private void EnableDarkMode(IntPtr hwnd, bool option)
    {
        // if (AllowDarkModeForWindow == nullptr) {
        //     return;
        // }
        // AllowDarkModeForWindow(hwnd, enable ? TRUE : FALSE);
    }

    private string _webview2RuntimePath;

    public void SetWebView2RuntimePath(string runtimePath)
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
        if (_webViewController is not null)
        {
            DLLImports.GetClientRect(_hwnd, out var rect);
            _webViewController.Bounds = new Rectangle(new Point(0, 0), new Size(rect.Right - rect.Left, rect.Bottom - rect.Top));
        }
    }

    public void FocusWebView2()
    {
        if (_webViewController is not null)
        {
            _webViewController.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
        }
    }

    public void NotifyWebView2WindowMove()
    {
        if (_webViewController is not null)
        {
            _webViewController.NotifyParentWindowPositionChanged();
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
        Console.WriteLine("install webview");
        // const wchar_t* srcURL = L"https://go.microsoft.com/fwlink/p/?LinkId=2124703";
        // const wchar_t* destFile = L"MicrosoftEdgeWebview2Setup.exe";
        //
        // if (S_OK == URLDownloadToFile(NULL, srcURL, destFile, 0, NULL))
        // {
        //     LPWSTR command = new wchar_t[100]{ L"MicrosoftEdgeWebview2Setup.exe\0" };	//add these switches? /silent /install
        //
        //     STARTUPINFO si;
        //     PROCESS_INFORMATION pi;
        //
        //     ZeroMemory(&si, sizeof(si));
        //     si.cb = sizeof(si);
        //     ZeroMemory(&pi, sizeof(pi));
        //
        //     bool success = CreateProcess(
        //         NULL,		// No module name (use command line)
        //         command,	// Command line
        //         NULL,       // Process handle not inheritable
        //         NULL,       // Thread handle not inheritable
        //         FALSE,      // Set handle inheritance to FALSE
        //         0,          // No creation flags
        //         NULL,       // Use parent's environment block
        //         NULL,       // Use parent's starting directory
        //         &si,        // Pointer to STARTUPINFO structure
        //         &pi);		// Pointer to PROCESS_INFORMATION structure
        //
        //     if(success)
        //     {
        //         // wait for the installation to complete
        //         WaitForSingleObject(pi.hProcess, INFINITE);
        //         CloseHandle(pi.hProcess);
        //         CloseHandle(pi.hThread);
        //     }
        //
        //     return success;
        // }
        //
        // return false;
        return true;
    }

    private async Task AttachWebView()
    {
        var runtimePath = string.IsNullOrWhiteSpace(_webview2RuntimePath) ? _webview2RuntimePath : null;

        // size_t runtimePathLen = wcsnlen(_webview2RuntimePath, _countof(_webview2RuntimePath));
        // PCWSTR runtimePath = runtimePathLen > 0 ? &_webview2RuntimePath[0] : nullptr;
        //
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
            sb.Append(_browserControlInitParameters); //e.g.--hide-scrollbars
        }

        var options = new CoreWebView2EnvironmentOptions();
        options.AdditionalBrowserArguments = sb.ToString();
        _webViewEnvironment = await CoreWebView2Environment.CreateAsync(runtimePath, _temporaryFilesPath, options);
        _webViewController = await _webViewEnvironment.CreateCoreWebView2ControllerAsync(_hwnd);
        _webViewWindow = _webViewController.CoreWebView2;

        var settings = _webViewWindow.Settings;
        settings.AreHostObjectsAllowed = true;
        settings.IsScriptEnabled = true;
        settings.AreDefaultScriptDialogsEnabled = true;
        settings.IsWebMessageEnabled = true;

        var webtoken = await _webViewWindow.AddScriptToExecuteOnDocumentCreatedAsync(
            "window.external = { sendMessage: function(message) { window.chrome.webview.postMessage(message); }, receiveMessage: function(callback) { window.chrome.webview.addEventListener(\'message\', function(e) { console.log(e.data); callback(e.data); }); } };");
        _webViewWindow.WebMessageReceived += (_, args) =>
        {
            Console.WriteLine(args.TryGetWebMessageAsString());
            var message = args.TryGetWebMessageAsString();
            _WebMessageReceivedCallback(message);
        };

        _webViewWindow.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        _webViewWindow.WebResourceRequested += (_, args) =>
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

                    var response = _webViewEnvironment.CreateWebResourceResponse(
                        memoryStream,
                        200,
                        "OK",
                        $"Content-Type: {contentType}");

                    args.Response = response;
                }
            }
        };

        _webViewWindow?.PermissionRequested += (sender, args) =>
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
            // MessageBox(nullptr, L"Neither StartUrl nor StartString was specified", L"Native Initialization Failed", MB_OK);
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

        if (_webViewController is null)
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

    public override bool GetTransparentEnabled()
    {
        return _webViewController!.DefaultBackgroundColor.A == 0;
    }

    public override bool GetContextMenuEnabled()
    {
        return _webViewWindow!.Settings.AreDefaultContextMenusEnabled;
    }

    public override bool GetDevToolsEnabled()
    {
        return _webViewWindow!.Settings.AreDevToolsEnabled;
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

    public Point GetPosition()
    {
        var rect = new RECT();
        DLLImports.GetWindowRect(_hwnd, out rect);
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
        var rect = new RECT();
        DLLImports.GetWindowRect(_hwnd, out rect);
        return new Size(rect.Width, rect.Height);
    }

    public override string GetTitle()
    {
        return _windowTitle;
    }

    public override bool GetTopmost()
    {
        var styles = DLLImports.GetWindowLong(_hwnd, Constants.GWL_STYLE);
        if ((styles & Constants.WS_EX_TOPMOST) != 0)
        {
            return true;
        }

        return false;
    }

    public override int GetZoom()
    {
        var rawValue = _webViewController!.ZoomFactor;
        rawValue = (rawValue * 100.0) + 0.5;
        return (int) rawValue;
    }

    public override bool GetIgnoreCertificateErrorsEnabled()
    {
        return _ignoreCertificateErrorsEnabled;
    }

    public override void NavigateToString(string content)
    {
        _webViewWindow!.NavigateToString(content);
    }

    public override void NavigateToUrl(string url)
    {
        _webViewWindow!.Navigate(url);
    }

    public override void Restore()
    {
        DLLImports.ShowWindow(_hwnd, unchecked((int) Constants.SW_RESTORE));
    }

    public override void SendWebMessage(string message)
    {
        _webViewWindow!.PostWebMessageAsString(message);
    }

    public override void SetTransparentEnabled(bool enabled)
    {
        var background = _webViewController!.DefaultBackgroundColor;
        _webViewController!.DefaultBackgroundColor = Color.FromArgb(enabled ? 0 : 255, background);
        _webViewWindow!.Reload();
    }

    public override void SetContextMenuEnabled(bool enabled)
    {
        _webViewWindow!.Settings.AreDefaultContextMenusEnabled = enabled;
        _webViewWindow.Reload();
    }

    public override void SetDevToolsEnabled(bool enabled)
    {
        _webViewWindow!.Settings.AreDevToolsEnabled = enabled;
        _webViewWindow.Reload();
    }

    public override void SetIconFile(string filename)
    {
        // HICON iconSmall = (HICON)LoadImage(NULL, filename, IMAGE_ICON, 16, 16, LR_LOADFROMFILE | LR_LOADTRANSPARENT | LR_SHARED);
        // HICON iconBig = (HICON)LoadImage(NULL, filename, IMAGE_ICON, 32, 32, LR_LOADFROMFILE | LR_LOADTRANSPARENT | LR_SHARED);
        //
        // if (iconSmall && iconBig)
        // {
        //     SendMessage(_hWnd, WM_SETICON, ICON_SMALL, (LPARAM)iconSmall);
        //     SendMessage(_hWnd, WM_SETICON, ICON_BIG, (LPARAM)iconBig);
        // }
        //
        // this->_iconFileName = filename;
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

    public void SetPosition(Point position)
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
        var style = DLLImports.GetWindowLongPtr(_hwnd, Constants.GWL_STYLE);
        if (topmost)
        {
            style |= Constants.WS_EX_TOPMOST;
        }
        else
        {
            style &= (~Constants.WS_EX_TOPMOST);
        }

        DLLImports.SetWindowLong(_hwnd, Constants.GWL_STYLE, style);
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
        _webViewController.ZoomFactor = zoom / 100.0;
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

        var msg = new MSG();

        while (DLLImports.GetMessage(out msg, IntPtr.Zero, 0, 0))
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

    public void AddFilters()
    {
        // std::vector<COMDLG_FILTERSPEC> specs;
        // for (int i = 0; i < filterCount; i++) {
        //     auto* filter = new wchar_t[MAX_PATH];
        //     AutoString wFilter = wndInstance->ToUTF16String(filters[i]);
        //     wcscpy_s(filter, MAX_PATH, wFilter);
        //
        //     const wchar_t* filterName = wcstok_s(filter, L"|", &filter);
        //     const wchar_t* filterPattern = filter;
        //     COMDLG_FILTERSPEC spec;
        //     spec.pszName = filterName;
        //     spec.pszSpec = filterPattern;
        //     specs.push_back(spec);
        // }
        // pfd->SetFileTypes(filterCount, specs.data());
    }

    public string GetResults()
    {
        // IShellItemArray* psiResults = nullptr;
        // *hr = pfd->GetResults(&psiResults);
        // if (SUCCEEDED(*hr)) {
        //     DWORD count = 0;
        //     psiResults->GetCount(&count);
        //     if (count > 0) {
        //         *resultCount = static_cast<int>(count);
        //         auto** result = new wchar_t* [count];
        //         for (DWORD i = 0; i < count; ++i) {
        //             IShellItem* psiItem = nullptr;
        //             *hr = psiResults->GetItemAt(i, &psiItem);
        //             if (SUCCEEDED(*hr)) {
        //                 PWSTR pszName = nullptr;
        //                 *hr = psiItem->GetDisplayName(SIGDN_FILESYSPATH, &pszName);
        //                 if (SUCCEEDED(*hr)) {
        //                     const auto len = wcslen(pszName);
        //                     result[i] = new wchar_t[len + 1];
        //                     wcscpy_s(result[i], len + 1, pszName);
        //                     CoTaskMemFree(pszName);
        //                 }
        //                 psiItem->Release();
        //             }
        //         }
        //         psiResults->Release();
        //         pfd->Release();
        //         return result;
        //     }
        //     psiResults->Release();
        // }
        // pfd->Release();
        //
        // return nullptr;
        return "";
    }

    public override string ShowOpenFile(string title, string path, bool multiSelect, string[] filters, int filterCount, out int resultCount)
    {
        // HRESULT hr;
        // title = _window->ToUTF16String(title);
        // defaultPath = _window->ToUTF16String(defaultPath);
        //
        // auto* pfd = Create<IFileOpenDialog>(&hr, title, defaultPath);
        //
        // if (SUCCEEDED(hr)) {
        //     AddFilters(pfd, filters, filterCount, _window);
        //
        //     DWORD dwOptions;
        //     pfd->GetOptions(&dwOptions);
        //     dwOptions |= FOS_FILEMUSTEXIST | FOS_NOCHANGEDIR;
        //     if (multiSelect) {
        //         dwOptions |= FOS_ALLOWMULTISELECT;
        //     }
        //     else {
        //         dwOptions &= ~FOS_ALLOWMULTISELECT;
        //     }
        //     pfd->SetOptions(dwOptions);
        //
        //     hr = pfd->Show(_window->getHwnd());
        //     if (SUCCEEDED(hr)) {
        //         return GetResults(pfd, &hr, resultCount);
        //     }
        //     pfd->Release();
        // }
        // return nullptr;
        resultCount = 0;
        return "";
    }

    public override string ShowOpenFolder(string title, string path, bool multiSelect, out int resultCount)
    {
        // HRESULT hr;
        // title = _window->ToUTF16String(title);
        // defaultPath = _window->ToUTF16String(defaultPath);
        //
        // auto* pfd = Create<IFileOpenDialog>(&hr, title, defaultPath);
        //
        // if (SUCCEEDED(hr)) {
        //     DWORD dwOptions;
        //     pfd->GetOptions(&dwOptions);
        //     dwOptions |= FOS_PICKFOLDERS | FOS_NOCHANGEDIR;
        //     if (multiSelect) {
        //         dwOptions |= FOS_ALLOWMULTISELECT;
        //     }
        //     else {
        //         dwOptions &= ~FOS_ALLOWMULTISELECT;
        //     }
        //     pfd->SetOptions(dwOptions);
        //
        //     hr = pfd->Show(_window->getHwnd());
        //     if (SUCCEEDED(hr)) {
        //         return GetResults(pfd, &hr, resultCount);
        //     }
        //     pfd->Release();
        // }
        // return nullptr;
        resultCount = 0;
        return "";
    }

    public override string ShowSaveFile(string title, string path, string[] filters, int filterCount)
    {
        // HRESULT hr;
        // title = _window->ToUTF16String(title);
        // defaultPath = _window->ToUTF16String(defaultPath);
        // auto* pfd = Create<IFileSaveDialog>(&hr, title, defaultPath);
        // if (SUCCEEDED(hr)) {
        //     AddFilters(pfd, filters, filterCount, _window);
        //
        //     DWORD dwOptions;
        //     pfd->GetOptions(&dwOptions);
        //     dwOptions |= FOS_NOCHANGEDIR;
        //     pfd->SetOptions(dwOptions);
        //
        //     hr = pfd->Show(_window->getHwnd());
        //     if (SUCCEEDED(hr)) {
        //         IShellItem* psiResult = nullptr;
        //         hr = pfd->GetResult(&psiResult);
        //         if (SUCCEEDED(hr)) {
        //             wchar_t* result = nullptr;
        //             PWSTR pszName = nullptr;
        //             hr = psiResult->GetDisplayName(SIGDN_FILESYSPATH, &pszName);
        //             if (SUCCEEDED(hr)) {
        //                 const auto len = wcslen(pszName);
        //                 result = new wchar_t[len + 1];
        //                 wcscpy_s(result, len + 1, pszName);
        //                 CoTaskMemFree(pszName);
        //             }
        //             psiResult->Release();
        //             pfd->Release();
        //             return result;
        //         }
        //     }
        //     pfd->Release();
        // }
        // return nullptr;
        return "";
    }

    public override DialogResult ShowMessage(string title, string text, DialogButtons buttons, DialogIcon icon)
    {
        // title = _window->ToUTF16String(title);
        // text = _window->ToUTF16String(text);
        // NewStyleContext ctx;
        //
        // UINT flags = {};
        //
        // switch (icon) {
        //     case DialogIcon::Info:	   flags |= MB_ICONINFORMATION;	break;
        //     case DialogIcon::Warning:  flags |= MB_ICONWARNING;	    break;
        //     case DialogIcon::Error:	   flags |= MB_ICONERROR;	    break;
        //     case DialogIcon::Question: flags |= MB_ICONQUESTION;    break;
        // }
        //
        // switch (buttons) {
        //     case DialogButtons::Ok:               flags |= MB_OK;               break;
        //     case DialogButtons::OkCancel:         flags |= MB_OKCANCEL;         break;
        //     case DialogButtons::YesNo:			  flags |= MB_YESNO;			break;
        //     case DialogButtons::YesNoCancel:      flags |= MB_YESNOCANCEL;	    break;
        //     case DialogButtons::RetryCancel:	  flags |= MB_RETRYCANCEL;	    break;
        //     case DialogButtons::AbortRetryIgnore: flags |= MB_ABORTRETRYIGNORE; break;
        // }
        //
        // const auto result = MessageBoxW(_window->getHwnd(), text, title, flags);
        //
        // switch (result) {
        //     case IDCANCEL: return DialogResult::Cancel;
        //     case IDOK:     return DialogResult::Ok;
        //     case IDYES:    return DialogResult::Yes;
        //     case IDNO:     return DialogResult::No;
        //     case IDABORT:  return DialogResult::Abort;
        //     case IDRETRY:  return DialogResult::Retry;
        //     case IDIGNORE: return DialogResult::Ignore;
        //     default:	   return DialogResult::Cancel;
        // }
        return DialogResult.Ignore;
    }

    public override void Invoke(Action callback)
    {
        GCHandle callbackHandle = GCHandle.Alloc(callback);

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

public class InvokeWaitInfo
{
    public bool isCompleted;
    public object lockObject = new object();
    public ManualResetEvent completionNotifier = new ManualResetEvent(false);
}
