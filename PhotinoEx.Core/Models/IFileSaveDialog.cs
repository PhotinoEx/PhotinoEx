using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Models;

[ComImport]
[Guid("84BCCD23-5FDE-4CDB-AEA4-AF64B83D78AB")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IFileSaveDialog
{
    [PreserveSig] int Show(IntPtr hwndOwner);
    void SetFileTypes(uint c, [MarshalAs(UnmanagedType.LPArray)] ComDlgFilterSpec[] r);
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
    void SetCollectedProperties(IntPtr pList, int fAppendDefault);
    void GetProperties(out IntPtr ppStore);
    void ApplyProperties(IShellItem psi, IntPtr pStore, IntPtr hwnd, IntPtr pSink);
}
