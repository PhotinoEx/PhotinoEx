# macOS implementation status

## Completed

- [x] Replace the placeholder `MacPhotinoEx` constructor with a Cocoa/WKWebView-backed window.
- [x] Support parent windows and startup window/webview configuration.
- [x] Implement close, message loop, UI-thread invocation, navigation, and outbound web messages.
- [x] Implement position, size, title, resizable, minimized, maximized, fullscreen, topmost, and zoom state.
- [x] Implement dev tools, user agent, browser permissions, security settings, and notification forwarding.
- [x] Forward close, focus, move, resize, minimized, maximized, restored, and web-message callbacks.
- [x] Implement custom URL schemes and native file, folder, save, and message dialogs.
- [x] Implement monitor enumeration.
- [x] Retain native macOS text and file clipboard support.
- [x] Remove `NotImplementedException` placeholders from the macOS backend.

## Intentionally unsupported

- [ ] Outbound file dragging. AppKit requires an `NSDraggingSession` started from the active pointer event; the native Photino backend does not expose the required view and event handles. The macOS unsupported annotations remain.
- [ ] Browser autofill clearing. The native WKWebView backend currently exposes this as a no-op. The API is marked unsupported on macOS.
- [ ] System tray. No macOS tray adapter exists for `IPhotinoExTray`.

## Upstream WKWebView limitations

- Transparency is accepted as configuration but is not implemented by the native backend.
- Context-menu disabling is not implemented; context menus remain enabled.
- Smooth-scrolling configuration is not implemented and reports disabled.
- macOS reports a conventional 72 DPI rather than physical display DPI.

## Runtime validation required

The repository can compile the managed macOS adapter on other operating systems, but these checks require macOS hardware and a signed application bundle:

- Launch, close, parent/child windows, and application message-loop shutdown.
- Local Blazor host-page loading and custom-scheme resource requests.
- External navigation and safe web-message origin handling. The upstream callback does not supply a sender URI, so source-aware Blazor messages are currently rejected rather than trusted.
- Window state changes and all corresponding callbacks.
- Clipboard operations with text, one file, and multiple files.
- Open/save dialogs, cancellation, filters, and multi-selection.
- Notifications and the required notification permission flow.
- Intel and Apple Silicon native library loading.
