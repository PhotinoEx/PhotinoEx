# Native Clipboard

## Status

The native clipboard implementation is complete for text and copy-style file/directory lists:

- [x] Add native clipboard operations to the shared platform contract.
- [x] Expose text and file-list copying through `PhotinoExWindow`.
- [x] Implement Windows text copying with `CF_UNICODETEXT`.
- [x] Implement Windows file and directory copying with `CF_HDROP`.
- [x] Handle temporary Windows clipboard contention and native memory ownership.
- [x] Implement Linux text copying through GTK/GDK.
- [x] Implement Linux file and directory copying with `text/uri-list` for X11 and Wayland.
- [x] Implement macOS text and file URL formats through `NSPasteboard` interop.
- [x] Add the Blazor `IPhotinoExClipboard` service without JavaScript interop.
- [x] Disable JavaScript clipboard access by default.
- [x] Validate and normalize file and directory paths.
- [x] Cover null, empty, missing-path, file, and directory behavior with unit tests.
- [x] Add a sample multi-file picker and copy action.
- [ ] Exercise each native backend in platform GUI integration tests.
- [ ] Enable runtime macOS verification after the macOS window backend is implemented.

## API

Core applications use the initialized window directly:

```csharp
window.CopyTextToClipboard("Text to copy");
window.CopyFilesToClipboard([filePath, directoryPath]);
```

Blazor applications inject the registered service:

```razor
@inject PhotinoEx.Blazor.IPhotinoExClipboard Clipboard

@code {
    private ValueTask CopyText() => Clipboard.CopyTextAsync("Text to copy");

    private ValueTask CopyFiles() => Clipboard.CopyFilesAsync(
        ["/path/to/report.pdf", "/path/to/folder"]
    );
}
```

Paths must identify existing files or directories. They are converted to absolute paths and duplicate entries are removed before reaching the native backend.

Directories remain single clipboard entries. The destination file manager recursively copies their contents during paste, so the API does not need a recursive flag.

## Platform formats

| Platform | Text | Files and directories |
| --- | --- | --- |
| Windows | `CF_UNICODETEXT` | `CF_HDROP` with a Unicode `DROPFILES` payload |
| Linux | GDK text content | `text/uri-list` containing escaped absolute file URIs |
| macOS | `NSPasteboard` UTF-8 text | `NSURL` pasteboard objects |

All operations are dispatched through `PhotinoExWindow.Invoke`, keeping native clipboard calls on the platform UI thread. Clipboard access requires an initialized window.
