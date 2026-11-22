namespace PhotinoEx.Core.Models;

public sealed class FilesDroppedEventArgs(
    IReadOnlyList<string> paths,
    FileDragDropEffects effect,
    int clientX,
    int clientY
) : EventArgs
{
    public IReadOnlyList<string> Paths { get; } = paths?.ToArray()
        ?? throw new ArgumentNullException(nameof(paths));
    public FileDragDropEffects Effect { get; } = effect;
    public int ClientX { get; } = clientX;
    public int ClientY { get; } = clientY;
}
