# Repository audit checkpoint — 28/07

## Findings

1. **Done — `Location` setter ignores the requested value after initialization.**
   `PhotinoEx.Core/PhotinoExWindow.cs:808` calls `SetPosition(Location)` instead of `SetPosition(value)`, so the native window receives its current position.

2. **Done — Blazor trusts messages from any navigated page as local application messages.**
   `PhotinoEx.Blazor/PhotinoExWebViewManager.cs:43-54` accepts every webview message and assigns the trusted local origin unconditionally. External content could invoke the Blazor message channel if the webview navigates away.

3. **Done — Webview message-pump lifetime is unmanaged.**
   `PhotinoEx.Blazor/PhotinoExWebViewManager.cs:64` discards the pump task. Disposal completes the channel, but the infinite `ReadAsync()` loop can then fault. The window event subscription is also never removed.

4. **Done — Windows dialogs leak COM shell items.**
   `PhotinoEx.Core/Platform/Windows/Dialog/WinPhotinoExDialog.cs:50,101,174,192` creates `IShellItem` objects without releasing them. Only the dialogs are released.

5. **Done — The solution builds with 78 warnings.**
   The main issues are nullable-contract warnings throughout `PhotinoEx.Blazor` and a `WindowsBase` 4.0/5.0 conflict caused by WebView2's WPF assembly entering the cross-platform `net10.0` project.

6. **Done — Linux feature exceptions are misleading.**
   `PhotinoEx.Core/Platform/Linux/LinPhotinoEx.cs:505,512,604,745` says Linux is unsupported when only window positioning or topmost behavior is unsupported. These should be `PlatformNotSupportedException` messages describing the feature limitation.

7. **Done — A production exception is unprofessional and loses exception structure.**
   `PhotinoEx.Core/Platform/Linux/LinPhotinoEx.cs:373` should let the original exception propagate or wrap it with a useful message and the original exception as its inner exception.

8. **Done — Windows dialog methods are falsely asynchronous.**
   Methods beginning at `PhotinoEx.Core/Platform/Windows/Dialog/WinPhotinoExDialog.cs:16` execute synchronously and contain no `await`, despite their `Async` names.

## Unsupported platform attributes

No existing unsupported attribute can currently be removed:

- Linux position remains unsupported and throws at `PhotinoEx.Core/Platform/Linux/LinPhotinoEx.cs:502`.
- Linux topmost remains unsupported and throws at `PhotinoEx.Core/Platform/Linux/LinPhotinoEx.cs:601,742`.
- Centering is Windows-only and directly casts to `WinPhotinoEx` at `PhotinoEx.Core/PhotinoExWindow.cs:223-243`.

The stale comment claiming topmost is tested on Linux has been removed.

Platform attributes added:

- `WindowHandle` is marked `[SupportedOSPlatform("windows")]`.
- `ClearBrowserAutoFill` is marked unsupported on Linux.
- Outbound file dragging is marked unsupported on macOS across Core and Blazor APIs.

## Validation

- Solution build: succeeded with no warnings or errors.
- Unit tests: all 66 passed.
- GUI integration tests: not run; they require `PHOTINOEX_RUN_GUI_TESTS=1` and a suitable desktop session.
