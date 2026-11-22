namespace PhotinoEx.Core.Models;

public sealed class WebMessageReceivedEventArgs(Uri? source, string message) : EventArgs
{
    public Uri? Source { get; } = source;
    public string Message { get; } = message;
}
