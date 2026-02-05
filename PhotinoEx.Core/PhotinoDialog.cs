using PhotinoEx.Core.Enums;

namespace PhotinoEx.Core;

public abstract class PhotinoDialog
{
    public abstract string ShowOpenFile(string title, string path, bool multiSelect, string[] filters, int filterCount, out int resultCount);

    public abstract string ShowOpenFolder(string title, string path, bool multiSelect, out int resultCount);

    public abstract string ShowSaveFile(string title, string path, string[] filters, int filterCount);

    public abstract DialogResult ShowMessage(string title, string text, DialogButtons buttons, DialogIcon icon);
}
