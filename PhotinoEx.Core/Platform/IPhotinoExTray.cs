using PhotinoEx.Core.Models;

namespace PhotinoEx.Core.Platform;

public interface IPhotinoExTray
{
    event EventHandler<Exception>? UnhandledException;

    Task<IPhotinoExTrayIcon> RegisterAsync(TrayIconOptions options, CancellationToken cancellationToken = default);
    bool TryGet(string id, out IPhotinoExTrayIcon? icon);
    Task<bool> UnregisterAsync(string id);
    Task UnregisterAllAsync();
}
