using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Utils;

public class Constants
{
    public const uint WS_THICKFRAME = 0x00040000;
    public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    public const uint WS_VISIBLE = 0x10000000;
    public const uint WS_POPUP = 0x80000000;
    public const uint WS_MAXIMIZE = 0x01000000;
    public const uint WS_MINIMIZE = 0x20000000;
    public const uint WS_MINIMIZEBOX = 0x00020000;
    public const uint WS_MAXIMIZEBOX = 0x00010000;

    public const uint WS_EX_TOPMOST = 0x00000008;
    public const uint WS_EX_CONTROLPARENT = 0x00010000;
    public const uint WS_EX_APPWINDOW = 0x00040000;
    public const uint WS_EX_LAYERED = 0x00080000;
    public const uint WS_EX_NOREDIRECTIONBITMAP = 0x00200000;

    public const uint WM_CREATE = 0x0001;
    public const uint WM_DESTROY = 0x0002;
    public const uint WM_MOVE = 0x0003;
    public const uint WM_SIZE = 0x0005;
    public const uint WM_ACTIVATE = 0x0006;
    public const uint WM_PAINT = 0x000F;
    public const uint WM_CLOSE = 0x0010;
    public const uint WM_SETTINGCHANGE = 0x001A;
    public const uint WM_GETMINMAXINFO = 0x0024;
    public const uint WM_SETICON = 0x0080;
    public const uint WM_THEMECHANGED = 0x031A;
    public const uint WM_MOVING = 0x0216;
    public const uint WM_USER = 0x0400;

    public const uint WA_INACTIVE = 0x0005;

    public const uint CW_USEDEFAULT = 0x80000000;

    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;

    public const uint LWA_ALPHA = 0x2;

    public const int GCLP_HBRBACKGROUND = -10;

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    public const uint CS_VREDRAW = 0x0001;
    public const uint CS_HREDRAW = 0x0002;

    public const uint SIZE_RESTORED = 0;
    public const uint SIZE_MINIMIZED = 1;
    public const uint SIZE_MAXIMIZED = 2;

    public const uint SW_HIDE = 0;
    public const uint SW_NORMAL = 1;
    public const uint SW_MAXIMIZE = 3;
    public const uint SW_MINIMIZE = 6;
    public const uint SW_RESTORE = 9;
    public const uint SW_SHOWDEFAULT = 10;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;

    public const int HWND_NOTOPMOST = -2;
    public const int HWND_TOPMOST = -1;
    public const int HWND_TOP = 0;
    public const int HWND_BOTTOM = 1;

    public const uint PM_REMOVE = 0x0001;

    public const int ICON_SMALL = 0;
    public const int ICON_BIG = 1;
    public const int IMAGE_ICON = 1;

    public const int LR_LOADFROMFILE = 0x00000010;
    public const int LR_DEFAULTSIZE = 0x00000040;
    public const int LR_SHARED = 0x000008000;

    public const uint FOS_OVERWRITEPROMPT = 0x00000002;
    public const uint FOS_STRICTFILETYPES = 0x00000004;
    public const uint FOS_NOCHANGEDIR = 0x00000008;
    public const uint FOS_PICKFOLDERS = 0x00000020;
    public const uint FOS_FORCEFILESYSTEM = 0x00000040;
    public const uint FOS_ALLNONSTORAGEITEMS = 0x00000080;
    public const uint FOS_NOVALIDATE = 0x00000100;
    public const uint FOS_ALLOWMULTISELECT = 0x00000200;
    public const uint FOS_PATHMUSTEXIST = 0x00000800;
    public const uint FOS_FILEMUSTEXIST = 0x00001000;
    public const uint FOS_CREATEPROMPT = 0x00002000;
    public const uint FOS_SHAREAWARE = 0x00004000;
    public const uint FOS_NOREADONLYRETURN = 0x00008000;
    public const uint FOS_NOTESTFILECREATE = 0x00010000;
    public const uint FOS_HIDEMRUPLACES = 0x00020000;
    public const uint FOS_HIDEPINNEDPLACES = 0x00040000;
    public const uint FOS_NODEREFERENCELINKS = 0x00100000;
    public const uint FOS_DONTADDTORECENT = 0x02000000;
    public const uint FOS_FORCESHOWHIDDEN = 0x10000000;
    public const uint FOS_DEFAULTNOMINIMODE = 0x20000000;
    public const uint FOS_FORCEPREVIEWPANEON = 0x40000000;

    public const uint SIGDN_FILESYSPATH = 0x80058000;

    public const int S_OK = 0;
    public const int ERROR_CANCELLED = unchecked((int) 0x800704C7);


    public const uint MB_OK = 0x00000000;
    public const uint MB_OKCANCEL = 0x00000001;
    public const uint MB_ABORTRETRYIGNORE = 0x00000002;
    public const uint MB_YESNOCANCEL = 0x00000003;
    public const uint MB_YESNO = 0x00000004;
    public const uint MB_RETRYCANCEL = 0x00000005;

    public const uint MB_ICONERROR = 0x00000010;
    public const uint MB_ICONQUESTION = 0x00000020;
    public const uint MB_ICONWARNING = 0x00000030;
    public const uint MB_ICONINFORMATION = 0x00000040;

    public const int IDOK = 1;
    public const int IDCANCEL = 2;
    public const int IDABORT = 3;
    public const int IDRETRY = 4;
    public const int IDIGNORE = 5;
    public const int IDYES = 6;
    public const int IDNO = 7;

    public const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public static readonly Guid CLSID_FileOpenDialog = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
    public static readonly Guid CLSID_FileSaveDialog = new Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");
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

[ComImport]
[Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IFileOpenDialog
{
    [PreserveSig] int Show(IntPtr hwndOwner);
    void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
    void SetFileTypeIndex(uint iFileType);
    void GetFileTypeIndex(out uint piFileType);
    void Advise(IntPtr pfde, out uint pdwCookie);
    void Unadvise(uint dwCookie);
    void SetOptions(uint fos);
    void GetOptions(out uint pfos);
    void SetDefaultFolder(IShellItem psi);
    void SetFolder(IShellItem psi);
    void GetFolder(out IShellItem ppsi);
    void GetCurrentSelection(out IShellItem ppsi);
    void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
    void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
    void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
    void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
    void GetResult(out IShellItem ppsi);
    void AddPlace(IShellItem psi, int fdap);
    void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
    void Close(int hr);
    void SetClientGuid([In] ref Guid guid);
    void ClearClientData();
    void SetFilter(IntPtr pFilter);
    void GetResults(out IShellItemArray ppenum);
    void GetSelectedItems(out IShellItemArray ppsai);
}

[ComImport]
[Guid("84BCCEA3-5FE1-45D5-8AD3-A759A9BB5B50")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IFileSaveDialog
{
    [PreserveSig] int Show(IntPtr hwndOwner);
    void SetFileTypes(uint c, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] r);
    void SetFileTypeIndex(uint iFileType);
    void GetFileTypeIndex(out uint piFileType);
    void Advise(IntPtr pfde, out uint pdwCookie);
    void Unadvise(uint dwCookie);
    void SetOptions(uint fos);
    void GetOptions(out uint pfos);
    void SetDefaultFolder(IShellItem psi);
    void SetFolder(IShellItem psi);
    void GetFolder(out IShellItem ppsi);
    void GetCurrentSelection(out IShellItem ppsi);
    void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
    void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
    void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
    void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
    void GetResult(out IShellItem ppsi);
    void AddPlace(IShellItem psi, int fdap);
    void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
    void Close(int hr);
    void SetClientGuid([In] ref Guid guid);
    void ClearClientData();
    void SetFilter(IntPtr pFilter);
    void SetSaveAsItem(IShellItem psi);
    void SetProperties(IntPtr pStore);
    void SetCollectedProperties(IntPtr pList, [MarshalAs(UnmanagedType.Bool)] bool fAppendDefault);
    void GetProperties(out IntPtr ppStore);
    void ApplyProperties(IShellItem psi, IntPtr pStore, IntPtr hwnd, IntPtr pSink);
}

[ComImport]
[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItem
{
    void BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
    void GetParent(out IShellItem ppsi);
    void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
    void Compare(IShellItem psi, uint hint, out int piOrder);
}

[ComImport]
[Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItemArray
{
    void BindToHandler(IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, out IntPtr ppvOut);
    void GetPropertyStore(int flags, [In] ref Guid riid, out IntPtr ppv);
    void GetPropertyDescriptionList(IntPtr keyType, [In] ref Guid riid, out IntPtr ppv);
    void GetAttributes(int AttribFlags, uint sfgaoMask, out uint psfgaoAttribs);
    void GetCount(out uint pdwNumItems);
    void GetItemAt(uint dwIndex, out IShellItem ppsi);
    void EnumItems(out IntPtr ppenumShellItems);
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct COMDLG_FILTERSPEC
{
    [MarshalAs(UnmanagedType.LPWStr)] public string pszName;
    [MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
}

public enum SIGDN : uint
{
    FILESYSPATH = 0x80058000
}
