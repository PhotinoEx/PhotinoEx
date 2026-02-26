using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Models;

[StructLayout(LayoutKind.Sequential)]
public struct WndClassEx
{
    public uint cbSize;
    public uint style;
    public IntPtr lpfnWndProc;
    public int cbClsExtra;
    public int cbWndExtra;
    public IntPtr hInstance;
    public IntPtr hIcon;
    public IntPtr hCursor;
    public IntPtr hbrBackground;
    public IntPtr lpszMenuName;
    public string lpszClassName;
    public IntPtr hIconSm;
}
