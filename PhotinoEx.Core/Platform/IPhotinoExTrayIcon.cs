namespace PhotinoEx.Core.Platform;

public interface IPhotinoExTrayIcon
{
    public Task<IPhotinoExTrayIcon> CreateAsync();
    public Task<bool> DisposeAsync();
    public void SetVisibility(bool state);
    public void SetIconPath(string path);
    public void SetContextMenu(object menu);
}
