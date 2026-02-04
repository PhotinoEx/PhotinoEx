using PhotinoEx.Core.TempModels;
using Monitor = PhotinoEx.Core.Models.Monitor;

namespace PhotinoEx.Core;

public class PhotinoWindows : Photino
{
    public PhotinoWindows(PhotinoInitParams initParams)
    {
        throw new NotImplementedException();
    }

    private static HINSTANCE? _hInstance { get; set; }
    private HWND? _hWnd { get; set; }
    private WinToastHandler? _toastHandler { get; set; }
    private object? _webViewEnvironment { get; set; }
    private object? _webViewWindow { get; set; }
    private object? _webViewController { get; set; }

    public static void Register(HINSTANCE hInstance)
    {
        throw new NotImplementedException();
    }

    public static void SetWebView2RuntimePath(string runtimePath)
    {
        throw new NotImplementedException();
    }

    public HWND GetHwnd()
    {
        throw new NotImplementedException();
    }

    public void RefitContent()
    {
        throw new NotImplementedException();
    }

    public void FocusWebView2()
    {
        throw new NotImplementedException();
    }

    public void NotifyWebView2WindowMove()
    {
        throw new NotImplementedException();
    }

    public void GetNotificationEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public string ToUTF16String(string source)
    {
        throw new NotImplementedException();
    }

    public string ToUTF8string(string source)
    {
        throw new NotImplementedException();
    }

    private bool EnsureWebViewIsInstalled()
    {
        throw new NotImplementedException();
    }

    private bool InstallWebView2()
    {
        throw new NotImplementedException();
    }

    private void AttachWebView()
    {
        throw new NotImplementedException();
    }

    private bool ToWide(PhotinoInitParams initParams)
    {
        throw new NotImplementedException();
    }

    protected override void Show(bool isAlreadyShown)
    {
        throw new NotImplementedException();
    }

    public override void Center()
    {
        throw new NotImplementedException();
    }

    public override void ClearBrowserAutoFill()
    {
        throw new NotImplementedException();
    }

    public override void GetTransparentEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void GetContextMenuEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void GetDevToolsEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void GetFullScreen(bool fullScreen)
    {
        throw new NotImplementedException();
    }

    public override void GetGrantBrowserPermissions(bool grant)
    {
        throw new NotImplementedException();
    }

    public override string GetUserAgent()
    {
        throw new NotImplementedException();
    }

    public override void GetMediaAutoplayEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void GetFileSystemAccessEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void GetWebSecurityEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void GetJavascriptClipboardAccessEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void GetMediaStreamEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void GetSmoothScrollingEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override string GetIconFileName()
    {
        throw new NotImplementedException();
    }

    public override void GetMaximized(bool isMaximized)
    {
        throw new NotImplementedException();
    }

    public override void GetMinimized(bool isMinimized)
    {
        throw new NotImplementedException();
    }

    public override void GetPosition(int x, int y)
    {
        throw new NotImplementedException();
    }

    public override void GetResizable(bool resizable)
    {
        throw new NotImplementedException();
    }

    public override int GetScreenDpi()
    {
        throw new NotImplementedException();
    }

    public override void GetSize(int width, int height)
    {
        throw new NotImplementedException();
    }

    public override string GetTitle()
    {
        throw new NotImplementedException();
    }

    public override void GetTopmost(bool topmost)
    {
        throw new NotImplementedException();
    }

    public override void GetZoom(int zoom)
    {
        throw new NotImplementedException();
    }

    public override void GetIgnoreCertificateErrorsEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void NavigateToString(string content)
    {
        throw new NotImplementedException();
    }

    public override void NavigateToUrl(string url)
    {
        throw new NotImplementedException();
    }

    public override void Restore()
    {
        throw new NotImplementedException();
    }

    public override void SendWebMessage(string message)
    {
        throw new NotImplementedException();
    }

    public override void SetTransparentEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void SetContextMenuEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void SetDevToolsEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }

    public override void SetIconFile(string filename)
    {
        throw new NotImplementedException();
    }

    public override void SetFullScreen(bool fullScreen)
    {
        throw new NotImplementedException();
    }

    public override void SetMaximized(bool maximized)
    {
        throw new NotImplementedException();
    }

    public override void SetMaxSize(int width, int height)
    {
        throw new NotImplementedException();
    }

    public override void SetMinimized(bool minimized)
    {
        throw new NotImplementedException();
    }

    public override void SetMinSize(int width, int height)
    {
        throw new NotImplementedException();
    }

    public override void SetPosition(int x, int y)
    {
        throw new NotImplementedException();
    }

    public override void SetResizable(bool resizable)
    {
        throw new NotImplementedException();
    }

    public override void SetSize(int width, int height)
    {
        throw new NotImplementedException();
    }

    public override void SetTitle(string title)
    {
        throw new NotImplementedException();
    }

    public override void SetTopmost(bool topmost)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public override void AddCustomSchemeName(string scheme)
    {
        throw new NotImplementedException();
    }

    public override void GetAllMonitors(Func<Monitor, int> callback)
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
