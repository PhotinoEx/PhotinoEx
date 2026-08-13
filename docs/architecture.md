# PhotinoEx Architecture

## Purpose

PhotinoEx is a .NET 10 desktop UI host. It creates a native operating-system window, embeds the platform webview, and optionally runs a Blazor UI inside it.

The solution separates native window concerns from Blazor hosting:

```text
Application / Razor components
            |
            v
PhotinoEx.Blazor
  DI, static assets, Blazor rendering, JS/.NET messages
            |
            v
PhotinoEx.Core
  public window API and platform selection
            |
     +------+-------+
     |      |       |
  Windows  Linux   macOS
  Win32 +  GTK 4   stub
  WebView2 WebKit
```

## Solution structure

| Project | Role |
| --- | --- |
| `PhotinoEx.Core` | Public native-window API, startup configuration, platform abstraction, OS implementations, dialogs, and tray contracts. |
| `PhotinoEx.Blazor` | Blazor WebView integration, dependency injection, static-file handling, dispatching, synchronization, and web-message transport. |
| `PhotinoEx.Test` | Executable Razor sample used to exercise the libraries. It is not a unit-test project. |

`PhotinoEx.Blazor` references `PhotinoEx.Core`; `PhotinoEx.Test` references `PhotinoEx.Blazor`. All projects target .NET 10.

## Core layer

### Public facade

`PhotinoExWindow` is the main consumer-facing API. It owns:

- `PhotinoExInitParams`, which accumulates settings before native initialization.
- The selected `Platform.PhotinoEx` instance after initialization.
- Fluent window configuration and runtime window operations.
- Window lifecycle, native callbacks, web messages, custom URL schemes, dialogs, and notifications.
- Native text and file-list clipboard operations.
- UI-thread dispatch through `Invoke`.

Many properties have two phases: before initialization they update startup parameters; afterward they delegate to the native implementation. Operations that require a native window reject calls made before initialization.

`WaitForClose()` is the initialization boundary. It validates parameters, raises creation events, asks `PhotinoExFactory` for the OS implementation, and starts the single process-wide native message loop. The static message-loop guard means all windows share one loop.

### Platform abstraction

`Platform.PhotinoEx` defines the native operations required by `PhotinoExWindow`, including navigation, sizing, state, browser settings, callbacks, messaging, dialogs, notifications, and monitor information. `PhotinoExFactory` selects an implementation using `RuntimeInformation`:

| Platform | Implementation | Native stack | Current state |
| --- | --- | --- | --- |
| Windows | `WinPhotinoEx` | Win32 and Microsoft WebView2 | Implemented |
| Linux | `LinPhotinoEx` | GTK 4, WebKitGTK 6, Gio, and D-Bus | Implemented |
| macOS | `MacPhotinoEx` | Not yet integrated | Constructor and operations throw `NotImplementedException` |

Platform-specific code is isolated under `PhotinoEx.Core/Platform/<OS>`. Shared DTOs and interop models live under `PhotinoEx.Core/Models`.

### Native services

Dialogs implement `IPhotinoExDialog` and are attached to the platform instance. Windows and Linux provide implementations for file, folder, save, and message dialogs.

Tray support is represented by `IPhotinoExTray` and `IPhotinoExTrayIcon`. Windows uses the notification-area APIs and Linux uses the GTK application's D-Bus connection; macOS tray support remains pending. A platform instance exposes optional `Dialog` and `Tray` services, so callers must account for platform availability and initialization.

Clipboard operations are part of the platform contract and are exposed through `PhotinoExWindow`. Text and mixed file/directory lists use native OS clipboard formats; no browser Clipboard API or JavaScript interop is involved. Windows uses Win32 clipboard formats, Linux uses GTK/GDK, and macOS uses `NSPasteboard` interop. See [clipboard.md](clipboard.md).

## Blazor layer

`PhotinoExBlazorAppBuilder` is the composition entry point. `CreateDefault()` registers the Blazor desktop services; consumers add root components and optional services, then call `Build()`. Applications should pass a stable reverse-domain identity, for example `CreateDefault("com.example.myapp")`. When omitted, the entry assembly name is used.

`PhotinoExServiceCollectionExtensions.AddBlazorDesktop()` registers:

- A singleton `PhotinoExWindow` and `PhotinoExBlazorApp`.
- `PhotinoExWebViewManager` and Blazor WebView services.
- Root-component and JavaScript-component infrastructure.
- Dispatcher, synchronization context, HTTP handler, and `HttpClient`.
- `IPhotinoExClipboard`, which delegates Blazor clipboard requests to the native window.
- A physical `wwwroot` provider by default, or a caller-supplied `IFileProvider`.

`PhotinoExBlazorApp.Initialize()` connects these services, configures the main window, registers the local-resource scheme, and adds root components. `Run()` navigates to the configured start URL and enters `PhotinoExWindow.WaitForClose()`.

### Static content

`PhotinoExWebViewManager` derives from ASP.NET Core's `WebViewManager`. It resolves requests against the configured file provider and serves `index.html`, framework assets, application assets, and fallback routes without starting a web server.

The local origin includes the application identity so separately installed PhotinoEx applications do not share an origin:

- Windows uses `http://{applicationId}.localhost/` because WebView2 cannot perform top-level navigation to the custom scheme used here.
- Linux and macOS use `app-{applicationId}://localhost/` because their webviews do not intercept HTTP requests in the same way.

The same identity isolates the Windows WebView2 data directory and notification registration ID. On Linux it is also used as the GTK application ID. Explicitly configured data paths and notification IDs are preserved.

The test application copies `wwwroot` to its output directory, where the default physical file provider can serve it.

### JavaScript and .NET messaging

The embedded webview and Blazor renderer communicate with string messages:

```text
JavaScript / Blazor renderer
       |              ^
       | web message  | queued web message
       v              |
PhotinoExWebViewManager
       |              ^
       v              |
PhotinoExWindow -> platform webview
```

Inbound messages are raised through `PhotinoExWindow.WebMessageReceived` and passed to `WebViewManager.MessageReceived` on a single-thread scheduler. Outbound messages enter an unbounded channel; one message pump sends them sequentially through `PhotinoExWindow.SendWebMessageAsync()` to the native webview.

The manager currently assigns all inbound messages the trusted local origin. Its source notes that the native layer does not report the true message origin, so navigation to untrusted external content would cross that trust boundary.

## Runtime sequence

1. The executable creates a `PhotinoExBlazorAppBuilder`.
2. The executable registers Razor root components and application services.
3. `Build()` creates the service provider and initializes `PhotinoExBlazorApp`.
4. The app configures `PhotinoExWindow` and registers the local resource handler.
5. `Run()` asks `PhotinoExWebViewManager` to navigate to the start URL.
6. `WaitForClose()` validates startup parameters and creates the Windows, Linux, or macOS platform object.
7. The platform creates its native window and webview, then starts the shared message loop.
8. Resource requests are served from the configured `IFileProvider`; web messages connect the Blazor renderer to the native webview.
9. Closing the main window ends the platform loop and returns from `Run()`.

## Threading and lifecycle constraints

- Construct and initialize `PhotinoExWindow` on the intended UI thread. It records the creating managed thread ID for dispatch decisions.
- Native mutations from other threads pass through `Invoke`.
- Only one native message loop is started for the process; child or additional windows reuse it.
- Startup-only settings must be applied before `WaitForClose()` creates the native instance.
- Custom schemes registered before initialization are copied into startup parameters, with a limit of 16.
- The Blazor outbound message pump lives for the lifetime of `PhotinoExWebViewManager` and completes its channel during disposal.

## Extension points

- Add fluent and runtime features to `PhotinoExWindow`, then extend the abstract `Platform.PhotinoEx` contract and each supported OS implementation.
- Add platform-native services behind shared interfaces such as `IPhotinoExDialog` and `IPhotinoExTray`.
- Supply a custom `IFileProvider` to host embedded or non-filesystem Blazor assets.
- Register application dependencies through `PhotinoExBlazorAppBuilder.Services`.
- Register root Razor components through `PhotinoExBlazorAppBuilder.RootComponents`.
- Use custom schemes for application-owned resources outside the Blazor host pipeline.

## Key dependencies

| Dependency | Purpose |
| --- | --- |
| `Microsoft.Web.WebView2` | Windows browser control. |
| `GirCore.Gtk-4.0` | Linux native application and window APIs. |
| `GirCore.WebKit-6.0` | Linux embedded browser. |
| `Microsoft.AspNetCore.Components.WebView` | Blazor rendering and WebView integration. |
