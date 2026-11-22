using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using PhotinoEx.Core;
using PhotinoEx.Core.Models;

namespace PhotinoEx.Blazor;

public sealed class PhotinoExFileDragDrop : IPhotinoExFileDragDrop, IDisposable
{
    private readonly PhotinoExWindow _window;

    public PhotinoExFileDragDrop(PhotinoExWindow window)
    {
        _window = window;
        _window.FilesDropped += OnFilesDropped;
    }

    public event EventHandler<FilesDroppedEventArgs>? FilesDropped;

    [UnsupportedOSPlatform("macos")]
    public Task<FileDragDropEffects> BeginDragAsync(
        IReadOnlyList<string> paths,
        FileDragDropEffects allowedEffects = FileDragDropEffects.Copy,
        CancellationToken cancellationToken = default
    ) => _window.BeginFileDragAsync(paths, allowedEffects, cancellationToken);

    public void Dispose() => _window.FilesDropped -= OnFilesDropped;

    private void OnFilesDropped(object? sender, FilesDroppedEventArgs args) =>
        FilesDropped?.Invoke(this, args);
}
