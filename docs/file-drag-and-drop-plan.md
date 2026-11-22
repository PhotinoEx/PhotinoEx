# Native File Drag-and-Drop Plan

## Goal

Support files and directories moving between PhotinoEx and desktop applications without browser clipboard or drag-and-drop APIs.

- **Inbound:** receive paths dropped onto a PhotinoEx window.
- **Outbound:** start a native drag from PhotinoEx and expose paths to the destination application.
- Default to copy semantics. Directories remain single entries; the destination handles recursion.

## Proposed API

```csharp
window.FilesDropped += (_, args) => HandleFiles(args.Paths);

var result = await window.BeginFileDragAsync(
    paths,
    FileDragDropEffects.Copy,
    cancellationToken
);
```

```csharp
public sealed record FilesDroppedEventArgs(
    IReadOnlyList<string> Paths,
    FileDragDropEffects Effect,
    int ClientX,
    int ClientY
);

[Flags]
public enum FileDragDropEffects
{
    None = 0,
    Copy = 1,
    Move = 2,
    Link = 4,
}
```

Outbound dragging must start from a real pointer gesture. `BeginFileDragAsync` should fail clearly when called outside a valid gesture or before window initialization.

## Phase 1: Contracts and validation

- [x] Add shared drag/drop effects and inbound event models.
- [x] Add inbound registration and outbound start methods to the platform contract.
- [x] Expose `FilesDropped` and `BeginFileDragAsync` through `PhotinoExWindow`.
- [x] Normalize paths, remove duplicates, and reject blank or missing entries.
- [x] Define copy, move, link, cancellation, and unsupported-effect behavior.
- [x] Keep clipboard and drag/drop APIs independent.

Completion requires platform-neutral contracts with unit tests for validation and effect negotiation.

## Phase 2: Windows backend

- [ ] Initialize and revoke OLE drag/drop for each window.
- [ ] Implement inbound `IDropTarget` handling for `CF_HDROP`.
- [ ] Convert inbound screen coordinates to client coordinates.
- [x] Implement outbound `IDataObject` with Unicode `CF_HDROP`.
- [x] Implement `IDropSource` and call `DoDragDrop` on the STA UI thread.
- [x] Map native `DROPEFFECT_*` results to the shared effects.
- [ ] Release COM objects and global memory on every result path.

Completion requires dragging files and folders both ways between the sample and Explorer, Notepad, and another drop-capable application.

## Phase 3: Linux backend

- [x] Attach a GTK/GDK drop target accepting GDK file lists and their serialized `text/uri-list` form.
- [x] Let GIO decode escaped file URIs and omit non-local URI schemes from the resulting path list.
- [x] Attach a native drag source and publish a GDK file-list value.
- [x] Use GDK file-list serialization to provide the `text/uri-list` interoperability format.
- [x] Start outbound dragging only from the active GTK pointer gesture.
- [x] Map GDK copy, move, link, and cancellation results.
- [x] Keep providers alive until the drag completes.
- [ ] Verify X11 and Wayland behavior separately.

Completion requires inbound and outbound file/folder dragging with Dolphin and Nautilus on X11 and Wayland.

## Phase 4: macOS backend

Deferred. The existing platform methods remain explicit `PlatformNotSupportedException` stubs.

- [ ] Register the window for file URL drag types.
- [ ] Receive inbound `NSDraggingInfo` file URLs and coordinates.
- [ ] Create outbound `NSDraggingItem` values backed by file URLs.
- [ ] Begin an `NSDraggingSession` from the originating mouse event.
- [ ] Map AppKit copy, move, link, and cancellation results.
- [ ] Validate ownership and autorelease behavior.

Completion requires Finder interoperability after the macOS window backend is operational.

## Phase 5: Blazor integration

- [x] Add an injectable `IPhotinoExFileDragDrop` service.
- [x] Route inbound drops to .NET without exposing browser filesystem objects.
- [x] Associate outbound drags with a specific rendered element and pointer gesture.
- [x] Provide a component/helper for draggable file rows.
- [x] Ensure event subscriptions are removed when components or services are disposed.

The bridge may use internal web messages to identify the source element and gesture, but paths and native drag data remain in .NET/native code.

Completion requires a Blazor page that receives desktop file drops and drags selected files into another application.

## Phase 6: Sample and tests

- [x] Add inbound drop-zone and outbound draggable-file examples to the sample app.
- [x] Add contract tests for path validation, URI conversion, effects, cancellation, and event lifetime.
- [ ] Add Windows integration tests for `CF_HDROP` and effect negotiation.
- [x] Add Linux tests for effect mapping; native format negotiation remains a desktop check.
- [ ] Add user-gesture desktop checks for Wayland and macOS.
- [ ] Test files, folders, mixed selections, empty files, Unicode, spaces, and special characters.
- [ ] Test source-window closure and missing-file races during a drag.
- [x] Document which GUI checks cannot run in headless CI.

Completion requires repeatable automated coverage plus documented manual checks for security-restricted desktop gestures.

### Manual desktop checks

Trusted drag gestures cannot be synthesized reliably in headless CI. Before release, run the sample against Explorer on Windows and Dolphin and Nautilus on both X11 and Wayland. Check files, folders, mixed selections, Unicode, spaces, special characters, cancellation, source-window closure, and files removed after selection. macOS checks remain deferred with Phase 4.

## Constraints

- Native desktop security rules prohibit synthesizing trusted drag gestures on some systems.
- A move result reports the destination's chosen effect; PhotinoEx must not delete source files itself.
- Inbound paths are untrusted input and must not be opened automatically.
- Virtual files that do not yet exist on disk are out of scope for the initial implementation.
- Drag images and custom non-file payloads are follow-up features.
