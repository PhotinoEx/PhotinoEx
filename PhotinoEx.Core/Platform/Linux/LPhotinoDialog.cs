

using PhotinoEx.Core.Enums;

namespace PhotinoEx.Core.Platform.Linux;

public class LPhotinoDialog : PhotinoDialog
{
    public LPhotinoDialog()
    {
    }

    public override string ShowOpenFile(string title, string path, bool multiSelect, string[] filters, int filterCount, out int resultCount)
    {
        var result = $"{title}:{path}:{multiSelect}:{string.Concat(filters)}:{filterCount}";
        Console.WriteLine(result);
        resultCount = 1;
        return result;
    }

    public override string ShowOpenFolder(string title, string path, bool multiSelect, out int resultCount)
    {
        var result = $"{title}:{path}:{multiSelect}";
        Console.WriteLine(result);
        resultCount = 1;
        return result;
    }

    public override string ShowSaveFile(string title, string path, string[] filters, int filterCount)
    {
        var result = $"{title}:{path}:{string.Concat(filters)}:{filterCount}";
        Console.WriteLine(result);
        return result;
    }

    public override DialogResult ShowMessage(string title, string text, DialogButtons buttons, DialogIcon icon)
    {
        Console.WriteLine($"{title}:{text}:{buttons}:{icon}");
        return DialogResult.Ok;
    }
}
