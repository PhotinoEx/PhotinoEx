using PhotinoEx.Core.Enums;

namespace PhotinoEx.Core;

public class PhotinoDialog
{
    // Windows Only
    public PhotinoDialog(Photino window)
    {
        _window = window;
    }

    // Linux and Apple Only
    public PhotinoDialog()
    {
    }

    // Windows Only
    private Photino? _window { get; set; }

    public string ShowOpenFile(string title, string path, bool multiSelect, string filters, int filterCount, int resultCount)
    {
        throw new NotImplementedException();
    }

    public string ShowOpenFolder(string title, string path, bool multiSelect, int resultCount)
    {
        throw new NotImplementedException();
    }

    public string ShowSaveFile(string title, string path, string filters, int filterCount)
    {
        throw new NotImplementedException();
    }

    public string ShowMessage(string title, string text, DialogButtons buttons, DialogIcon icon)
    {
        throw new NotImplementedException();
    }
}
