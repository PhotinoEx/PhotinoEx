using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using PhotinoEx.Core.Models;

namespace PhotinoEx.Blazor;

public interface IPhotinoExFileDragDrop
{
    event EventHandler<FilesDroppedEventArgs>? FilesDropped;

    [UnsupportedOSPlatform("macos")]
    Task<FileDragDropEffects> BeginDragAsync(
        IReadOnlyList<string> paths,
        FileDragDropEffects allowedEffects = FileDragDropEffects.Copy,
        CancellationToken cancellationToken = default
    );
}
