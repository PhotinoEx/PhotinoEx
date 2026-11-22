namespace PhotinoEx.Core;

public sealed class TrayIconException : Exception
{
    public TrayIconException(string message)
        : base(message) { }

    public TrayIconException(string message, Exception innerException)
        : base(message, innerException) { }
}
