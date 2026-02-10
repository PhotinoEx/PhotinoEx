using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Utils;

public class Constants
{
    public const int WS_THICKFRAME = 0x00040000;
    public const int WS_OVERLAPPEDWINDOW = 0x00CF0000;
    public const int WS_VISIBLE = 0x10000000;
    public const int WS_POPUP = unchecked((int)0x80000000);
    public const int WS_MAXIMIZE = 0x01000000;
    public const int WS_MINIMIZE = 0x20000000;
    public const int WS_MINIMIZEBOX = 0x00020000;
    public const int WS_MAXIMIZEBOX = 0x00010000;

    public const int WS_EX_TOPMOST = 0x00000008;
    public const int WS_EX_CONTROLPARENT = 0x00010000;
    public const int WS_EX_APPWINDOW = 0x00040000;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;

    public const int WM_CREATE = 0x0001;
    public const int WM_DESTROY = 0x0002;
    public const int WM_MOVE = 0x0003;
    public const int WM_SIZE = 0x0005;
    public const int WM_ACTIVATE = 0x0006;
    public const int WM_PAINT = 0x000F;
    public const int WM_CLOSE = 0x0010;
    public const int WM_SETTINGCHANGE = 0x001A;
    public const int WM_GETMINMAXINFO = 0x0024;
    public const int WM_THEMECHANGED = 0x031A;
    public const int WM_MOVING = 0x0216;
    public const int WM_USER = 0x0400;

    public const int WA_INACTIVE = 0x0005;

    public const int CW_USEDEFAULT = unchecked((int)0x80000000);

    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;

    public const int LWA_ALPHA = 0x2;

    public const int GCLP_HBRBACKGROUND = -10;

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    public const int CS_VREDRAW = 0x0001;
    public const int CS_HREDRAW = 0x0002;

    public const int SIZE_RESTORED = 0;
    public const int SIZE_MINIMIZED = 1;
    public const int SIZE_MAXIMIZED = 2;

    public const int SW_HIDE = 0;
    public const int SW_NORMAL = 1;
    public const int SW_MAXIMIZE = 3;
    public const int SW_MINIMIZE = 6;
    public const int SW_RESTORE = 9;
    public const int SW_SHOWDEFAULT = 10;

    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOMOVE = 0x0002;
    public const int SWP_NOZORDER = 0x0004;

    public const int HWND_NOTOPMOST = -2;
    public const int HWND_TOPMOST = -1;
    public const int HWND_TOP = 0;
    public const int HWND_BOTTOM = 1;
}

[StructLayout(LayoutKind.Sequential)]
public struct WNDCLASSEX
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

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct PAINT
{
    public IntPtr hdc;
    public bool fErase;
    public RECT rcPaint;
    public bool fRestore;
    public bool fIncUpdate;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] rgbReserved;
}

[StructLayout(LayoutKind.Sequential)]
public struct MSG
{
    public IntPtr hwnd;
    public uint message;
    public IntPtr wParam;
    public IntPtr lParam;
    public uint time;
    public POINT pt;
}


public struct RGBA
{
    public byte Red { get; set; }
    public byte Green { get; set; }
    public byte Blue { get; set; }
    public byte Alpha { get; set; }

    public static RGBA NewRGBA(byte red, byte green, byte blue, byte alpha)
    {
        return new RGBA
        {
            Red = red,
            Green = green,
            Blue = blue,
            Alpha = alpha,
        };
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct MINMAXINFO
{
    public POINT PtReserved { get; set; }
    public POINT ptMaxSize { get; set; }
    public POINT ptMaxPosition { get; set; }
    public POINT ptMinTrackSize { get; set; }
    public POINT ptMaxTrackSize { get; set; }
}
