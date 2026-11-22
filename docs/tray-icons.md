# Tray Icon Implementation Design

## Goals

Provide a cross-platform tray API that lets an application:

- Register one or more uniquely identified tray icons.
- Handle primary click, double-click, and context-menu opening.
- Show a native menu on right-click.
- Handle native menu-item activation in .NET.
- Update icon, tooltip, visibility, and menu after registration.
- Unregister icons deterministically and clean them up when the application exits.

The implementation must use the operating system's notification-area APIs. The menu must be native rather than HTML rendered inside the webview.

## Current state

Windows and Linux tray support is implemented. Both platforms use the shared `IPhotinoExTray` and `IPhotinoExTrayIcon` contracts, support registration and lifecycle updates, route click events, and build native menus from the shared menu model.

- Windows uses `Shell_NotifyIcon`, a dedicated native message window, and `TrackPopupMenuEx`. Icons currently require `.ico` files.
- Linux registers a StatusNotifierItem and exports a `com.canonical.dbusmenu` menu over D-Bus.
- macOS remains pending because its window backend is not yet usable.

The former hard-coded `PhotinoExWindow.ActivateTrayAndIcon()` helper has been removed in favor of the registration API below.

## Public API

Keep platform details internal and expose strongly typed options, menus, and events from `PhotinoEx.Core`.

```csharp
public sealed record TrayIconOptions(
    string Id,
    string IconPath,
    string? ToolTip = null,
    TrayMenu? Menu = null,
    bool IsVisible = true);

public enum TrayMouseButton
{
    Primary,
    Secondary,
    Middle
}

public sealed class TrayIconEventArgs : EventArgs
{
    public required TrayMouseButton Button { get; init; }
    public int ClickCount { get; init; }
}

public interface IPhotinoExTrayIcon : IAsyncDisposable
{
    string Id { get; }
    bool IsVisible { get; }

    event EventHandler<TrayIconEventArgs>? Clicked;
    event EventHandler? MenuOpening;

    Task SetIconAsync(string iconPath);
    Task SetToolTipAsync(string? toolTip);
    Task SetVisibleAsync(bool visible);
    Task SetMenuAsync(TrayMenu? menu);
}

public interface IPhotinoExTray
{
    Task<IPhotinoExTrayIcon> RegisterAsync(
        TrayIconOptions options,
        CancellationToken cancellationToken = default);

    bool TryGet(string id, out IPhotinoExTrayIcon? icon);
    Task<bool> UnregisterAsync(string id);
    Task UnregisterAllAsync();
}
```

Registration creates the native object before returning. A duplicate ID throws `InvalidOperationException`; invalid IDs, missing icon files, or unsupported image formats throw argument exceptions. Native registration failures throw a `TrayIconException` with the platform error as its inner exception.

Expose the tray from the initialized window:

```csharp
public IPhotinoExTray Tray =>
    _instance?.Tray
    ?? throw new InvalidOperationException(
        "Tray icons are unavailable before the window is initialized.");
```

Because the native application exists only after platform initialization, normal registration occurs in `WindowCreated`:

```csharp
window.WindowCreated += async (_, _) =>
{
    var icon = await window.Tray.RegisterAsync(new(
        Id: "main",
        IconPath: iconPath,
        ToolTip: "PhotinoEx",
        Menu: BuildTrayMenu()));

    icon.Clicked += (_, e) =>
    {
        if (e.Button == TrayMouseButton.Primary)
        {
            window.Restore();
        }
    };
};
```

The Blazor layer requires no separate tray implementation. Applications can resolve or retain their `PhotinoExWindow` and use this Core API. A later convenience registration service may wrap it, but should not create a second abstraction.

## Native menu model

Do not accept `object` for menus. A shared model gives every backend the same behavior and makes updates testable.

```csharp
public sealed class TrayMenu
{
    public IReadOnlyList<TrayMenuItem> Items { get; init; } = [];
}

public abstract record TrayMenuItem(string Id);

public sealed record TrayMenuCommand(
    string Id,
    string Text,
    Func<CancellationToken, Task> Activated,
    bool IsEnabled = true,
    bool IsChecked = false) : TrayMenuItem(Id);

public sealed record TrayMenuSeparator(string Id) : TrayMenuItem(Id);

public sealed record TraySubmenu(
    string Id,
    string Text,
    IReadOnlyList<TrayMenuItem> Items,
    bool IsEnabled = true) : TrayMenuItem(Id);
```

Requirements:

- IDs are unique within the complete menu tree and remain stable across updates.
- Commands, separators, checked items, disabled items, and nested submenus are supported.
- Menu command handlers run through the captured application synchronization context.
- Exceptions from handlers are reported through a tray-level `UnhandledException` event; they must not escape a native callback boundary.
- `MenuOpening` fires immediately before display so the app can replace or update the menu.
- Platforms that cannot distinguish right-click should still open the menu through their standard native interaction.

The first version can rebuild the native menu when `SetMenuAsync` is called. Incremental menu diffing is unnecessary until profiling shows it is needed.

## Click behavior

Normalize native interactions as follows:

| Native interaction | Managed behavior |
| --- | --- |
| Primary click | Raise `Clicked` with `Primary` and `ClickCount = 1`. |
| Primary double-click | Raise `Clicked` with `Primary` and `ClickCount = 2`; suppress the delayed single-click when the backend can do so reliably. |
| Secondary click | Raise `Clicked` with `Secondary`, raise `MenuOpening`, then show the native menu. |
| Middle click | Raise `Clicked` with `Middle`; do not open the menu. |
| Menu item selected | Invoke the matching command's `Activated` delegate. |

The public contract must not promise exact pointer coordinates or identical click ordering where the OS does not provide them. Menu display follows each platform's conventions.

## Platform backends

### Windows

Add `WinPhotinoExTray` and `WinPhotinoExTrayIcon` under `Platform/Windows/Tray`.

- Register icons with `Shell_NotifyIcon` using `NOTIFYICONDATA` and `NIM_ADD`.
- Set `NIF_MESSAGE`, `NIF_ICON`, `NIF_TIP`, and `NIF_GUID`; use a stable GUID derived from the application and icon ID.
- Route the callback message through the existing Win32 window procedure or a dedicated hidden message window.
- Map `WM_LBUTTONUP`, `WM_LBUTTONDBLCLK`, `WM_RBUTTONUP`, and `WM_MBUTTONUP` to managed click events.
- Build an `HMENU` for each context menu, call `SetForegroundWindow`, then `TrackPopupMenuEx` on secondary click.
- Map returned command IDs back to stable menu-item IDs; destroy replaced menus with `DestroyMenu`.
- Load `.ico` directly. For PNG support, decode it and create a correctly scaled `HICON`; destroy owned icon handles after replacement or removal.
- Re-register icons after the shell's `TaskbarCreated` message, because Explorer restarts remove notification icons.
- Remove icons with `NIM_DELETE` during disposal.

### Linux

Complete `LinPhotinoExTrayIcon` as a StatusNotifierItem.

- Own a unique D-Bus name and export `org.kde.StatusNotifierItem` at `/StatusNotifierItem`.
- Register the exported object with `org.kde.StatusNotifierWatcher`.
- Publish icon name or pixmap, tooltip, status, title, and menu object path properties.
- Map `Activate`, `SecondaryActivate`, and `Scroll` calls to managed events. `SecondaryActivate` raises `MenuOpening` before the host requests or displays the menu.
- Export the menu through `com.canonical.dbusmenu` so the desktop shell renders it natively.
- Translate menu layout requests and `Event` activation calls to the shared menu model.
- Emit the required property/layout change signals after icon, tooltip, visibility, or menu updates.
- Unexport D-Bus objects and release the bus name during disposal.

StatusNotifierItem support depends on the desktop environment. GNOME commonly requires an AppIndicator extension, while KDE and several other desktops support it directly. Registration failure should be surfaced rather than silently reported as success.

### macOS

Implement `MacPhotinoExTray` and `MacPhotinoExTrayIcon` when the macOS window backend is implemented.

- Create an `NSStatusItem` through `NSStatusBar.SystemStatusBar`.
- Configure its `NSStatusBarButton` image, tooltip, target, and action.
- Attach an `NSMenu` populated from the shared menu model.
- Use the button event or separate action routing to distinguish primary and secondary clicks where available.
- Invoke menu commands through selectors mapped back to menu-item IDs.
- Remove the status item from `NSStatusBar` during disposal.
- Use template images where appropriate so the icon follows menu-bar appearance and scale.

On macOS, assigning an `NSMenu` changes normal click behavior: the system may open the menu for the standard click instead of only right-click. The backend should preserve native convention and document this platform difference rather than simulate a non-native menu.

## Ownership and lifecycle

The platform object owns one tray registry, and the registry owns every registered native icon.

```text
PhotinoExWindow
  -> Platform.PhotinoEx
       -> IPhotinoExTray
            -> icon ID -> IPhotinoExTrayIcon -> native handle/object
```

- Registration and all updates marshal to the native UI thread.
- `UnregisterAsync` removes the icon from the registry and disposes its native resources.
- Calling `DisposeAsync` directly must also remove the icon from the registry, or the registry must remove stale disposed entries before reuse of an ID.
- Disposal is idempotent.
- Platform shutdown calls `UnregisterAllAsync` before tearing down the UI loop or D-Bus connection.
- Closing or hiding the last window does not implicitly remove a tray icon. This permits tray-resident applications.
- Process exit is not a substitute for cleanup, especially on Windows where stale icons can remain until Explorer refreshes.

## Threading and event safety

Native callbacks arrive on platform UI threads. Capture the window's synchronization context during registration and dispatch public events and command handlers through it. Never hold a registry or menu lock while invoking application code.

Use a per-icon asynchronous gate to serialize registration, updates, and disposal. Reads such as `TryGet` may use the existing concurrent dictionary, but state changes and native calls must not race disposal.

Event handlers may close windows, unregister the icon, or replace its menu. Backends must therefore resolve callback state before invoking user code and tolerate disposal during the callback.

## Possible future: animated icons

GIF files are not animated automatically by the native tray APIs. The current Windows backend accepts `.ico` files, while Linux uses a shell-resolved image path and is intended for static images such as PNG. Passing a GIF path is therefore not a portable animation mechanism.

Animated tray icons could be added explicitly:

1. Decode the source GIF into frames and retain each frame's delay.
2. Schedule frame changes while the icon is visible.
3. On Windows, convert each frame to an `HICON` and update it with `Shell_NotifyIcon`.
4. On Linux, publish each frame as StatusNotifierItem pixmap data and emit the icon-change signal. Pixmaps avoid desktop-shell path caching.
5. Pause animation when hidden and release all decoded frames and native handles during disposal.

The API should describe this as animation rather than GIF support so it can also accept APNG, WebP, or an application-provided frame sequence later. Frame rate should be capped to avoid unnecessary CPU use and excessive shell updates.

Taskbar or dock icons have the same basic limitation: assigning a GIF does not make the native application icon animate. PhotinoEx could update a window/taskbar icon frame by frame, but shells may cache or throttle changes and rapid updates may flicker. Platform-native attention mechanisms should be preferred where possible, such as Windows taskbar progress and overlay icons or the macOS dock badge. Any future taskbar animation API should be separate from tray animation because the native handles, lifecycle, and platform behavior differ.

## Suggested implementation order

- [x] Add the shared options, event arguments, menu model, exception, and revised interfaces.
- [x] Expose `PhotinoExWindow.Tray` and remove the hard-coded `ActivateTrayAndIcon()` path.
- [x] Complete Linux StatusNotifierItem registration and lifecycle.
- [x] Add Linux click routing and D-Bus menu export.
- [x] Add the Windows notification icon, click routing, and native popup menu.
- [x] Add deterministic lifecycle cleanup and error propagation.
- [ ] Re-register Windows icons after Explorer's `TaskbarCreated` message.
- [ ] Add macOS tray support after its platform window implementation is usable.

## Verification

Shared tests should cover:

- Rejecting blank and duplicate icon IDs.
- Register/get/unregister behavior and ID reuse after disposal.
- Menu validation, nested command lookup, enabled and checked state, and separators.
- Event translation for primary, secondary, double, and middle clicks.
- Menu handler exceptions and disposal during an event callback.
- Concurrent update and disposal behavior.

Platform smoke tests should verify:

- The icon appears, changes, hides, shows, and disappears on cleanup.
- Primary click reaches the application.
- Secondary click opens an OS-native menu at the expected location.
- Every menu command invokes exactly once.
- Disabled, checked, separator, and submenu rendering is native.
- DPI/theme changes use a correctly scaled icon.
- Linux session restart/disconnect and Windows Explorer restart are handled or reported cleanly.
- Multiple icons remain independent and retain unique event/menu routing.
