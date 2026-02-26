using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Models;

[StructLayout(LayoutKind.Sequential)]
public struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}
