using System.Drawing;
using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Models;

[StructLayout(LayoutKind.Sequential)]
public struct MinMaxInfo
{
    public Point PtReserved { get; set; }
    public Point ptMaxSize { get; set; }
    public Point ptMaxPosition { get; set; }
    public Point ptMinTrackSize { get; set; }
    public Point ptMaxTrackSize { get; set; }
}
