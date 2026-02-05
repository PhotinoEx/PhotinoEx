using PhotinoEx.Core.Models;
using PhotinoEx.Core.TempModels;
using Monitor = PhotinoEx.Core.Models.Monitor;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PhotinoEx.Core.Platform.Windows;

public class WPhotino : Photino
{
    public WPhotino(PhotinoInitParams initParams)
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

    public override void Show()
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

    public override void Close()
    {
        throw new NotImplementedException();
    }

    public override bool GetTransparentEnabled()
    {
        throw new NotImplementedException();
    }

    public override bool GetContextMenuEnabled()
    {
        throw new NotImplementedException();
    }

    public override bool GetDevToolsEnabled()
    {
        throw new NotImplementedException();
    }

    public override bool GetFullScreen()
    {
        throw new NotImplementedException();
    }

    public override bool GetGrantBrowserPermissions()
    {
        throw new NotImplementedException();
    }

    public override string GetUserAgent()
    {
        throw new NotImplementedException();
    }

    public override bool GetMediaAutoplayEnabled()
    {
        throw new NotImplementedException();
    }

    public override bool GetFileSystemAccessEnabled()
    {
        throw new NotImplementedException();
    }

    public override bool GetWebSecurityEnabled()
    {
        throw new NotImplementedException();
    }

    public override bool GetJavascriptClipboardAccessEnabled()
    {
        throw new NotImplementedException();
    }

    public override bool GetMediaStreamEnabled()
    {
        throw new NotImplementedException();
    }

    public override bool GetSmoothScrollingEnabled()
    {
        throw new NotImplementedException();
    }

    public override bool GetNotificationsEnabled()
    {
        throw new NotImplementedException();
    }

    public override string GetIconFileName()
    {
        throw new NotImplementedException();
    }

    public override bool GetMaximized()
    {
        throw new NotImplementedException();
    }

    public override bool GetMinimized()
    {
        throw new NotImplementedException();
    }

    public override Point GetPosition()
    {
        throw new NotImplementedException();
    }

    public override bool GetResizable()
    {
        throw new NotImplementedException();
    }

    public override uint GetScreenDpi()
    {
        throw new NotImplementedException();
    }

    public override Size GetSize()
    {
        throw new NotImplementedException();
    }

    public override string GetTitle()
    {
        throw new NotImplementedException();
    }

    public override bool GetTopmost()
    {
        throw new NotImplementedException();
    }

    public override int GetZoom()
    {
        throw new NotImplementedException();
    }

    public override bool GetIgnoreCertificateErrorsEnabled()
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

    public override void SetMaxSize(Size size)
    {
        throw new NotImplementedException();
    }

    public override void SetMinimized(bool minimized)
    {
        throw new NotImplementedException();
    }

    public override void SetMinSize(Size size)
    {
        throw new NotImplementedException();
    }

    public override void SetPosition(Point position)
    {
        throw new NotImplementedException();
    }

    public override void SetResizable(bool resizable)
    {
        throw new NotImplementedException();
    }

    public override void SetSize(Size size)
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
