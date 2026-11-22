using System.Drawing;
using System.Runtime.Versioning;
using PhotinoEx.Core;
using PhotinoEx.Blazor;
using Xunit;

namespace PhotinoEx.Core.Tests;

[SupportedOSPlatform("windows")]
public sealed class PhotinoExWindowTests
{
    [Theory]
    [InlineData("http://localhost/page", "http://localhost/", true)]
    [InlineData("http://localhost:80/page", "http://localhost/", true)]
    [InlineData("https://localhost/page", "http://localhost/", false)]
    [InlineData("http://example.test/page", "http://localhost/", false)]
    [InlineData("http://localhost:8080/page", "http://localhost/", false)]
    public void BlazorMessagesRequireTheApplicationOrigin(string source, string appBaseUri, bool expected)
    {
        Assert.Equal(expected, PhotinoExWebViewManager.IsSameOrigin(new Uri(source), new Uri(appBaseUri)));
    }

    [Fact]
    public void DefaultsAreConfiguredForAUsableWindow()
    {
        var window = new PhotinoExWindow();

        Assert.True(window.Resizable);
        Assert.True(window.ContextMenuEnabled);
        Assert.True(window.DevToolsEnabled);
        Assert.True(window.GrantBrowserPermissions);
        Assert.True(window.UseOsDefaultSize);
        Assert.Equal("PhotinoEx", window.Title);
        Assert.Equal("PhotinoEx WebView", window.UserAgent);
        Assert.Equal(100, window.Zoom);
        Assert.Equal(2, window.LogVerbosity);
        Assert.Null(window.Parent);
        Assert.Throws<InvalidOperationException>(() => window.Tray);
    }

    [Fact]
    public void ApplicationIdentityIsValidatedAndNormalised()
    {
        Assert.Equal("com.example.my-app", PhotinoExApplicationIdentity.Normalize(" COM.Example.My-App "));
        Assert.Equal("com.photinoex.myapp", PhotinoExApplicationIdentity.GetLinuxApplicationId("myapp"));
        Assert.Throws<ArgumentException>(() => PhotinoExApplicationIdentity.Normalize("not valid"));
    }

    [Fact]
    public void ApplicationIdentityIsolatesDerivedWindowDefaults()
    {
        var window = new PhotinoExWindow();

        window.ApplicationId = "com.example.one";

        Assert.Equal("com.example.one", window.ApplicationId);
        Assert.Equal("com.example.one", window.NotificationRegistrationId);
        if (PhotinoExWindow.IsWindowsPlatform)
        {
            Assert.EndsWith(Path.Combine("PhotinoEx", "com.example.one"), window.TemporaryFilesPath);
        }
    }

    [Fact]
    public void ApplicationIdentityDoesNotReplaceExplicitWindowSettings()
    {
        var window = new PhotinoExWindow
        {
            NotificationRegistrationId = "custom-notifications",
            TemporaryFilesPath = "custom-profile"
        };

        window.ApplicationId = "com.example.two";

        Assert.Equal("custom-notifications", window.NotificationRegistrationId);
        Assert.Equal("custom-profile", window.TemporaryFilesPath);
    }

    [Fact]
    public void ApplicationIdentityCreatesAnIsolatedBlazorOrigin()
    {
        var first = PhotinoExWebViewManager.CreateAppBaseUri("com.example.one");
        var second = PhotinoExWebViewManager.CreateAppBaseUri("com.example.two");

        Assert.NotEqual(first, second);
        Assert.Equal(first.Scheme == "http" ? "com.example.one.localhost" : "localhost", first.Host);
    }

    [Fact]
    public void ParentAndIdsAreTracked()
    {
        var parent = new PhotinoExWindow();
        var child = new PhotinoExWindow(parent);

        Assert.Same(parent, child.Parent);
        Assert.NotEqual(parent.Id, child.Id);
    }

    [Fact]
    public void FluentConfigurationUpdatesPropertiesAndReturnsWindow()
    {
        var window = new PhotinoExWindow();

        var result = window
            .SetChromeless(true)
            .SetTransparent(true)
            .SetContextMenuEnabled(false)
            .SetDevToolsEnabled(false)
            .SetFullScreen(true)
            .SetGrantBrowserPermissions(false)
            .SetUserAgent("test-agent")
            .SetMediaAutoplayEnabled(false)
            .SetFileSystemAccessEnabled(false)
            .SetWebSecurityEnabled(false)
            .SetJavascriptClipboardAccessEnabled(false)
            .SetMediaStreamEnabled(false)
            .SetSmoothScrollingEnabled(false)
            .SetIgnoreCertificateErrorsEnabled(true)
            .SetNotificationsEnabled(false)
            .SetResizable(false)
            .SetTopMost(true)
            .SetUseOsDefaultLocation(true)
            .SetUseOsDefaultSize(false)
            .SetLogVerbosity(4);

        Assert.Same(window, result);
        Assert.True(window.Chromeless);
        Assert.True(window.Transparent);
        Assert.False(window.ContextMenuEnabled);
        Assert.False(window.DevToolsEnabled);
        Assert.True(window.FullScreen);
        Assert.False(window.GrantBrowserPermissions);
        Assert.Equal("test-agent", window.UserAgent);
        Assert.False(window.MediaAutoplayEnabled);
        Assert.False(window.FileSystemAccessEnabled);
        Assert.False(window.WebSecurityEnabled);
        Assert.False(window.JavascriptClipboardAccessEnabled);
        Assert.False(window.MediaStreamEnabled);
        Assert.False(window.SmoothScrollingEnabled);
        Assert.True(window.IgnoreCertificateErrorsEnabled);
        Assert.False(window.NotificationsEnabled);
        Assert.False(window.Resizable);
        Assert.True(window.Topmost);
        Assert.True(window.UseOsDefaultLocation);
        Assert.False(window.UseOsDefaultSize);
        Assert.Equal(4, window.LogVerbosity);
    }

    [Fact]
    public void GeometryFluentMethodsUpdateStartupGeometry()
    {
        var window = new PhotinoExWindow()
            .SetSize(800, 600)
            .SetLocation(new Point(20, 30))
            .SetMinSize(200, 150)
            .SetMaxSize(1200, 900)
            .SetZoom(125);

        Assert.Equal(new Size(800, 600), window.Size);
        Assert.Equal(new Point(20, 30), window.Location);
        Assert.Equal(new Point(200, 150), window.MinSize);
        Assert.Equal(new Point(1200, 900), window.MaxSize);
        Assert.Equal(125, window.Zoom);
    }

    [Fact]
    public void IndividualPropertiesUpdateStartupConfiguration()
    {
        var window = new PhotinoExWindow
        {
            Centered = true,
            Height = 480,
            Width = 640,
            Left = 11,
            Top = 12,
            MinWidth = 100,
            MinHeight = 101,
            MaxWidth = 900,
            MaxHeight = 901,
            IconFile = "icon.png",
            BrowserControlInitParameters = "parameters",
            NotificationRegistrationId = "notification-id",
            TemporaryFilesPath = "temp",
        };

        Assert.True(window.Centered);
        Assert.Equal(new Size(640, 480), window.Size);
        Assert.Equal(new Point(11, 12), window.Location);
        Assert.Equal(new Point(100, 101), window.MinSize);
        Assert.Equal(new Point(900, 901), window.MaxSize);
        Assert.Equal("icon.png", window.IconFile);
        Assert.Equal("parameters", window.BrowserControlInitParameters);
        Assert.Equal("notification-id", window.NotificationRegistrationId);
        Assert.Equal("temp", window.TemporaryFilesPath);
    }

    [Fact]
    public void NativeStateIsUnavailableBeforeInitialization()
    {
        var window = new PhotinoExWindow();

        Assert.Throws<ApplicationException>(() => window.Monitors);
        Assert.Throws<ApplicationException>(() => window.MainMonitor);
        Assert.Throws<ApplicationException>(() => window.ScreenDpi);
        if (!PhotinoExWindow.IsWindowsPlatform)
        {
            Assert.Throws<PlatformNotSupportedException>(() => window.WindowHandle);
        }
    }

    [Fact]
    public void HandlerRegistrationUpdatesPublicHandlerProperties()
    {
        var window = new PhotinoExWindow();
        EventHandler<Point> location = (_, _) => { };
        EventHandler<Size> size = (_, _) => { };
        EventHandler simple = (_, _) => { };
        EventHandler<string> message = (_, _) => { };
        PhotinoExWindow.NetClosingDelegate closing = (_, _) => true;

        Assert.Same(window, window.RegisterLocationChangedHandler(location));
        Assert.Same(window, window.RegisterSizeChangedHandler(size));
        Assert.Same(window, window.RegisterFocusInHandler(simple));
        Assert.Same(window, window.RegisterFocusOutHandler(simple));
        Assert.Same(window, window.RegisterMaximizedHandler(simple));
        Assert.Same(window, window.RegisterRestoredHandler(simple));
        Assert.Same(window, window.RegisterMinimizedHandler(simple));
        Assert.Same(window, window.RegisterWebMessageReceivedHandler(message));
        Assert.Same(window, window.RegisterWindowClosingHandler(closing));
        Assert.Same(window, window.RegisterWindowCreatingHandler(simple));
        Assert.Same(window, window.RegisterWindowCreatedHandler(simple));

        Assert.Same(location, window.WindowLocationChangedHandler);
        Assert.Same(size, window.WindowSizeChangedHandler);
        Assert.Same(simple, window.WindowFocusInHandler);
        Assert.Same(simple, window.WindowFocusOutHandler);
        Assert.Same(simple, window.WindowMaximizedHandler);
        Assert.Same(simple, window.WindowRestoredHandler);
        Assert.Same(simple, window.WindowMinimizedHandler);
        Assert.Same(message, window.WebMessageReceivedHandler);
        Assert.Same(closing, window.WindowClosingHandler);
        Assert.Same(simple, window.WindowCreatingHandler);
        Assert.Same(simple, window.WindowCreatedHandler);
    }

    [Fact]
    public void ContentCanBeConfiguredBeforeInitialization()
    {
        var window = new PhotinoExWindow();

        Assert.Same(window, window.Load(new Uri("https://example.test/path")));
        Assert.Equal("https://example.test/path", window.StartUrl);
        Assert.Same(window, window.LoadRawString("<h1>Hello</h1>"));
        Assert.Equal("<h1>Hello</h1>", window.StartString);
    }

    [Fact]
    public void MissingLocalContentDoesNotChangeStartUrl()
    {
        var window = new PhotinoExWindow();

        Assert.Same(window, window.Load($"missing-{Guid.NewGuid():N}.html"));

        Assert.Equal(string.Empty, window.StartUrl);
    }

    [Fact]
    public void InvokeRunsImmediatelyOnCreatingThread()
    {
        var invoked = false;
        var window = new PhotinoExWindow();

        var result = window.Invoke(() => invoked = true);

        Assert.True(invoked);
        Assert.Same(window, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CustomSchemeRequiresAName(string? scheme)
    {
        var window = new PhotinoExWindow();

        Assert.Throws<ArgumentException>(() => window.RegisterCustomSchemeHandler(scheme!, HandleScheme));
    }

    [Fact]
    public void CustomSchemeRequiresAHandler()
    {
        var window = new PhotinoExWindow();

        Assert.Throws<ArgumentException>(() => window.RegisterCustomSchemeHandler("app", null!));
    }

    [Fact]
    public void CustomSchemeIsCaseInsensitiveAndCopiesContent()
    {
        var window = new PhotinoExWindow();
        window.RegisterCustomSchemeHandler("APP", HandleScheme);

        using var result = window.OnCustomScheme("app:resource", out var contentType);

        Assert.Equal("text/plain", contentType);
        Assert.Equal("app:resource", ReadFromStart(result));
    }

    [Theory]
    [InlineData("missing-colon")]
    [InlineData("unknown:resource")]
    public void InvalidCustomSchemeRequestIsRejected(string url)
    {
        var window = new PhotinoExWindow();

        Assert.Throws<ApplicationException>(() => window.OnCustomScheme(url, out _));
    }

    [Fact]
    public void AtMostSixteenCustomSchemesCanBeRegisteredBeforeInitialization()
    {
        var window = new PhotinoExWindow();
        for (var index = 0; index < 16; index++)
        {
            window.RegisterCustomSchemeHandler($"scheme{index}", HandleScheme);
        }

        Assert.Throws<ApplicationException>(() => window.RegisterCustomSchemeHandler("overflow", HandleScheme));
        Assert.Same(window, window.RegisterCustomSchemeHandler("scheme0", HandleScheme));
    }

    private static Stream HandleScheme(object? sender, string? scheme, string url, out string contentType)
    {
        contentType = "text/plain";
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(url));
    }

    private static string ReadFromStart(MemoryStream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
