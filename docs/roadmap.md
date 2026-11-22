# Implementation Roadmap

## Status

| Phase | Workstream | Status |
| --- | --- | --- |
| 1 | Clipboard integration tests | In progress |
| 2 | Linux clipboard compatibility | Pending |
| 3 | Clipboard read support | Pending |
| 4 | Clipboard hardening | Pending |
| 5 | Cross-platform CI | In progress |
| 6 | Tray completion | Pending |
| 7 | macOS window backend | In progress |
| 8 | Warning reduction and release readiness | In progress |

## Phase 1: Clipboard integration tests

Validate native clipboard behavior in real desktop sessions rather than relying only on contract tests.

- [x] Create a separate integration-test project and test category.
- [x] Keep integration tests excluded from the default headless unit-test run.
- [ ] Verify Windows Unicode text clipboard output.
- [ ] Verify Windows file and directory output through `CF_HDROP`.
- [ ] Verify Linux text output under X11.
- [ ] Verify Linux text output under Wayland through a user-triggered sample action.
- [ ] Verify Linux file and directory output with Nautilus and Dolphin.
- [x] Cover mixed files/directories, Unicode, spaces, `#`, and empty files.
- [x] Document local prerequisites and commands.
- [x] Record macOS tests as deferred until the macOS host is operational.

Completion requires repeatable passing clipboard smoke tests on Windows and Linux with failures reporting the platform and native format involved.

## Phase 2: Linux clipboard compatibility

Publish the formats expected by major Linux file managers while retaining the standard URI list.

- [ ] Publish `text/uri-list`.
- [ ] Publish `x-special/gnome-copied-files` with the `copy` action.
- [ ] Combine both formats in one GDK content provider.
- [ ] Confirm paste behavior in Nautilus and Dolphin on X11 and Wayland.
- [ ] Keep clipboard provider data alive for as long as GTK requires it.

Completion requires the same copied selection to paste correctly in Nautilus and Dolphin without application-specific branches in public APIs.

## Phase 3: Clipboard read support

Add native text and file-list reading without exposing browser clipboard APIs.

- [ ] Add `ReadTextAsync` and `ReadFilesAsync` to the shared contracts.
- [ ] Implement Windows format detection and reads.
- [ ] Implement asynchronous GTK/GDK reads.
- [ ] Implement `NSPasteboard` reads.
- [ ] Define empty, unavailable, cancelled, and unsupported-format behavior.
- [ ] Add Blazor service methods and unit tests.

Completion requires consistent results and cancellation semantics across supported platforms.

## Phase 4: Clipboard hardening

Exercise edge cases and make native failures actionable.

- [ ] Add a `ClipboardException` that preserves native error details.
- [ ] Test long paths, large selections, duplicates, non-ASCII text, and special characters.
- [ ] Verify Windows allocation ownership on every failure path.
- [ ] Verify Linux content-provider lifetime and disposal.
- [ ] Verify macOS Objective-C ownership and autorelease behavior.
- [ ] Define practical size limits or document platform limits.

Completion requires deterministic cleanup, actionable exceptions, and tests for known edge cases.

## Phase 5: Cross-platform CI

Build and test the repository continuously on its supported operating systems.

- [x] Add Windows, Ubuntu, and macOS build jobs.
- [ ] Run unit tests and collect coverage on every job.
- [ ] Publish test and coverage artifacts.
- [ ] Add optional GUI smoke-test jobs where desktop sessions are available.
- [x] Keep environment-dependent tests clearly separated from required headless checks.

Completion requires required build/unit-test jobs on all three operating systems and documented optional GUI jobs.

## Phase 6: Tray completion

Finish known tray lifecycle gaps and increase behavioral coverage.

- [ ] Re-register Windows tray icons after Explorer emits `TaskbarCreated`.
- [ ] Test native click and menu-command routing.
- [ ] Test concurrent updates and disposal.
- [ ] Test shell/session disconnect handling and cleanup.
- [ ] Add macOS tray support after Phase 7 provides a usable host.

Completion requires reliable Windows/Linux lifecycle recovery and tests for native event routing.

## Phase 7: macOS window backend

Replace the current stub with a usable native window and webview host.

- [x] Select and document the AppKit/WebKit binding approach.
- [x] Implement application and window lifecycle.
- [x] Embed and configure `WKWebView`.
- [x] Implement navigation, messaging, custom schemes, dialogs, and dispatch.
- [ ] Integrate existing clipboard operations and verify them on macOS.
- [ ] Add tray support and platform smoke tests.

Completion requires the sample application to start, render Blazor UI, exchange messages, use dialogs and clipboard operations, and close cleanly on macOS.

## Phase 8: Warning reduction and release readiness

Resolve actionable warnings and establish a clean packaging baseline.

- [x] Fix nullable warnings in dialogs, WebView initialization, and Linux native object handling.
- [x] Resolve the `WindowsBase` assembly-version conflict.
- [x] Address analyzer warnings in tests and platform annotations.
- [ ] Define which warnings fail CI.
- [x] Verify package metadata, licenses, symbols, and release builds.
- [x] Document supported platforms and known limitations.

Completion requires clean release builds under the agreed warning policy and reproducible package output.
