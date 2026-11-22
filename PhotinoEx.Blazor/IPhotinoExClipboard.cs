using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhotinoEx.Blazor;

/// <summary>Provides access to the system clipboard through the hosted webview.</summary>
public interface IPhotinoExClipboard
{
    /// <summary>Copies text to the system clipboard.</summary>
    ValueTask CopyTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Copies files or directories to the system clipboard.</summary>
    ValueTask CopyFilesAsync(
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default
    );
}
