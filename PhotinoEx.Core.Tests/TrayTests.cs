using PhotinoEx.Core.Models;
using PhotinoEx.Core.Platform;
using PhotinoEx.Core.Platform.Linux.Tray;
using Xunit;

namespace PhotinoEx.Core.Tests;

public sealed class TrayTests : IDisposable
{
    private readonly string _iconPath = Path.GetTempFileName();

    [Fact]
    public async Task RegisterGetAndUnregisterTracksIcon()
    {
        var tray = new FakeTray();
        var icon = await tray.RegisterAsync(new TrayIconOptions("main", _iconPath), TestContext.Current.CancellationToken);

        Assert.True(tray.TryGet("main", out var registered));
        Assert.Same(icon, registered);
        Assert.True(await tray.UnregisterAsync("main"));
        Assert.False(tray.TryGet("main", out _));
        Assert.True(((FakeIcon)icon).IsDisposed);
    }

    [Fact]
    public async Task DuplicateIdIsRejected()
    {
        var tray = new FakeTray();
        await tray.RegisterAsync(new TrayIconOptions("main", _iconPath), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tray.RegisterAsync(new TrayIconOptions("main", _iconPath), TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task IdCanBeReusedAfterUnregister()
    {
        var tray = new FakeTray();
        await tray.RegisterAsync(new TrayIconOptions("main", _iconPath), TestContext.Current.CancellationToken);
        await tray.UnregisterAsync("main");

        var replacement = await tray.RegisterAsync(new TrayIconOptions("main", _iconPath), TestContext.Current.CancellationToken);

        Assert.Equal("main", replacement.Id);
    }

    [Fact]
    public async Task BlankAndMissingIconInputsAreRejected()
    {
        var tray = new FakeTray();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            tray.RegisterAsync(new TrayIconOptions("", _iconPath), TestContext.Current.CancellationToken)
        );
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            tray.RegisterAsync(new TrayIconOptions("main", _iconPath + ".missing"), TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task NullAndBlankIconPathsAreRejected()
    {
        var tray = new FakeTray();

        await Assert.ThrowsAsync<ArgumentNullException>(() => tray.RegisterAsync(null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            tray.RegisterAsync(new TrayIconOptions("main", " "), TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task UnregisteringUnknownIdReturnsFalse()
    {
        var tray = new FakeTray();

        Assert.False(await tray.UnregisterAsync("unknown"));
    }

    [Fact]
    public void UnhandledExceptionIsForwardedToSubscribers()
    {
        var tray = new FakeTray();
        var expected = new InvalidOperationException("failure");
        Exception? reported = null;
        tray.UnhandledException += (_, exception) => reported = exception;

        tray.ReportUnhandledException(expected);

        Assert.Same(expected, reported);
    }

    [Fact]
    public async Task DuplicateNestedMenuIdIsRejected()
    {
        var menu = new TrayMenu
        {
            Items =
            [
                new TrayMenuCommand("same", "First", _ => Task.CompletedTask),
                new TraySubmenu("submenu", "More", [new TrayMenuSeparator("same")]),
            ],
        };

        var tray = new FakeTray();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            tray.RegisterAsync(new TrayIconOptions("main", _iconPath, Menu: menu), TestContext.Current.CancellationToken)
        );
    }

    [Theory]
    [InlineData("", "Command")]
    [InlineData("command", "")]
    public async Task BlankCommandFieldsAreRejected(string id, string text)
    {
        var menu = new TrayMenu
        {
            Items = [new TrayMenuCommand(id, text, _ => Task.CompletedTask)],
        };

        var tray = new FakeTray();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            tray.RegisterAsync(new TrayIconOptions("main", _iconPath, Menu: menu), TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task BlankSubmenuTextIsRejected()
    {
        var menu = new TrayMenu { Items = [new TraySubmenu("more", " ", [])] };

        var tray = new FakeTray();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            tray.RegisterAsync(new TrayIconOptions("main", _iconPath, Menu: menu), TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task UnregisterAllDisposesEveryIcon()
    {
        var tray = new FakeTray();
        var first = (FakeIcon)await tray.RegisterAsync(new TrayIconOptions("first", _iconPath), TestContext.Current.CancellationToken);
        var second = (FakeIcon)await tray.RegisterAsync(new TrayIconOptions("second", _iconPath), TestContext.Current.CancellationToken);

        await tray.UnregisterAllAsync();

        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
    }

    [Fact]
    public async Task LinuxMenuRefreshesConditionalCommandVisibility()
    {
        var visible = false;
        var menu = new TrayMenu
        {
            Items =
            [
                new TrayMenuCommand(
                    "conditional",
                    "Conditional",
                    _ => Task.CompletedTask,
                    GetState: _ => ValueTask.FromResult(new TrayMenuItemState(IsVisible: visible))
                ),
            ],
        };
        var owner = new LinPhotinoExTray();
        var icon = new LinPhotinoExTrayIcon(owner, new TrayIconOptions("main", _iconPath, Menu: menu), 1);
        var nativeMenu = new LinPhotinoExTrayMenu(owner, icon, menu);

        Assert.True(await nativeMenu.AboutToShowAsync(0));
        var hiddenLayout = await nativeMenu.GetLayoutAsync(0, -1, []);
        var hiddenItem = Assert.IsType<DBusMenuLayout>(Assert.Single(hiddenLayout.Layout.Children));
        Assert.False(Assert.IsType<bool>(hiddenItem.Properties["visible"]));

        visible = true;
        Assert.True(await nativeMenu.AboutToShowAsync(0));
        var visibleLayout = await nativeMenu.GetLayoutAsync(0, -1, []);
        var visibleItem = Assert.IsType<DBusMenuLayout>(Assert.Single(visibleLayout.Layout.Children));
        Assert.True(Assert.IsType<bool>(visibleItem.Properties["visible"]));
    }

    public void Dispose() => File.Delete(_iconPath);

    private sealed class FakeTray : PhotinoExTrayBase
    {
        protected override Task<IPhotinoExTrayIcon> CreateIconAsync(
            TrayIconOptions options,
            CancellationToken cancellationToken
        ) => Task.FromResult<IPhotinoExTrayIcon>(new FakeIcon(options));
    }

    private sealed class FakeIcon(TrayIconOptions options) : IPhotinoExTrayIcon
    {
        public string Id { get; } = options.Id;
        public bool IsVisible { get; private set; } = options.IsVisible;
        public bool IsDisposed { get; private set; }
        public event EventHandler<TrayIconEventArgs>? Clicked { add { } remove { } }
        public event EventHandler? MenuOpening { add { } remove { } }
        public Task SetIconAsync(string iconPath) => Task.CompletedTask;
        public Task SetToolTipAsync(string? toolTip) => Task.CompletedTask;
        public Task SetVisibleAsync(bool visible) { IsVisible = visible; return Task.CompletedTask; }
        public Task SetMenuAsync(TrayMenu? menu) => Task.CompletedTask;
        public ValueTask DisposeAsync() { IsDisposed = true; return ValueTask.CompletedTask; }
    }
}
