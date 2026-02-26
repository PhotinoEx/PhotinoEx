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

    public const int DWMWA_NCRENDERING_ENABLED = 1;
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_BORDER_COLOR = 34;
    public const int DWMWA_CAPTION_COLOR = 35;
    public const int DWMWA_TEXT_COLOR = 36;
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    public static readonly Guid CLSID_FileOpenDialog = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
    public static readonly Guid CLSID_FileSaveDialog = new Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");

    public const string WindowsTheme = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
}
