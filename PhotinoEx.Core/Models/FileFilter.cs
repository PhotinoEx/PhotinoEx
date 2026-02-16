namespace PhotinoEx.Core.Models;

public class FileFilter
{
    public string Name { get; set; }
    public string Spec { get; set; }

    public FileFilter(string name, string spec)
    {
        Name = name;
        Spec = spec;
    }
}
