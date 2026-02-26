using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Models;

[StructLayout(LayoutKind.Sequential)]
public struct Paint
{
    public IntPtr hdc;
    public bool fErase;
    public Rect rcPaint;
    public bool fRestore;
    public bool fIncUpdate;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] rgbReserved;
}
