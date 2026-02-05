using PhotinoEx.Core.Enums;

namespace PhotinoEx.Core.Platform.Windows;

public class WPhotinoDialog : PhotinoDialog
{
    public WPhotinoDialog(Photino window)
    {
        _window = window;
    }

    private Photino? _window { get; set; }

    public override string ShowOpenFile(string title, string path, bool multiSelect, string[] filters, int filterCount, out int resultCount)
    {
        throw new NotImplementedException();
    }

    public override string ShowOpenFolder(string title, string path, bool multiSelect, out int resultCount)
    {
        throw new NotImplementedException();
    }

    public override string ShowSaveFile(string title, string path, string[] filters, int filterCount)
    {
        throw new NotImplementedException();
    }

    public override DialogResult ShowMessage(string title, string text, DialogButtons buttons, DialogIcon icon)
    {
        throw new NotImplementedException();
    }
}
