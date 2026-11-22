using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotinoEx.Core;

namespace PhotinoEx.Blazor;

public sealed class PhotinoExClipboard(PhotinoExWindow window) : IPhotinoExClipboard
{
    public ValueTask CopyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();
        window.CopyTextToClipboard(text);
        return ValueTask.CompletedTask;
    }

    public ValueTask CopyFilesAsync(
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(paths);
        cancellationToken.ThrowIfCancellationRequested();
        window.CopyFilesToClipboard(paths);
        return ValueTask.CompletedTask;
    }
}
