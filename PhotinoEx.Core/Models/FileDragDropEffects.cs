namespace PhotinoEx.Core.Models;

[Flags]
public enum FileDragDropEffects
{
    None = 0,
    Copy = 1,
    Move = 2,
    Link = 4,
}
