using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Models;

[StructLayout(LayoutKind.Sequential)]
public struct COMDLG_FILTERSPEC
{
    [MarshalAs(UnmanagedType.LPWStr)] public string pszName;
    [MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
}

[ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItem
{
    [PreserveSig]
    int BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        out IntPtr ppv);

    [PreserveSig]
    int GetParent(out IShellItem ppsi);

    [PreserveSig]
    int GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

    [PreserveSig]
    int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

    [PreserveSig]
    int Compare(IShellItem psi, uint hint, out int piOrder);
}

[ComImport, Guid("b63ea76d-1f85-456f-a19c-48159efa858b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IShellItemArray
{
    [PreserveSig]
    int BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        out IntPtr ppv);

    [PreserveSig]
    int GetPropertyStore(int flags, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);

    [PreserveSig]
    int GetPropertyDescriptionList([MarshalAs(UnmanagedType.LPStruct)] Guid keyType, [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        out IntPtr ppv);

    [PreserveSig]
    int GetAttributes(uint dwAttribFlags, uint sfgaoMask, out uint psfgaoAttribs);

    [PreserveSig]
    int GetCount(out uint pdwNumItems);

    [PreserveSig]
    int GetItemAt(uint dwIndex, out IShellItem ppsi);

    [PreserveSig]
    int EnumItems(out IntPtr ppenumShellItems);
}

[ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IFileDialog
{
    [PreserveSig]
    int Show(IntPtr parent);

    [PreserveSig]
    int SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);

    [PreserveSig]
    int SetFileTypeIndex(uint iFileType);

    [PreserveSig]
    int GetFileTypeIndex(out uint piFileType);

    [PreserveSig]
    int Advise(IntPtr pfde, out uint pdwCookie);

    [PreserveSig]
    int Unadvise(uint dwCookie);

    [PreserveSig]
    int SetOptions(uint fos);

    [PreserveSig]
    int GetOptions(out uint fos);

    [PreserveSig]
    int SetDefaultFolder(IShellItem psi);

    [PreserveSig]
    int SetFolder(IShellItem psi);

    [PreserveSig]
    int GetFolder(out IShellItem ppsi);

    [PreserveSig]
    int GetCurrentSelection(out IShellItem ppsi);

    [PreserveSig]
    int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);

    [PreserveSig]
    int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

    [PreserveSig]
    int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

    [PreserveSig]
    int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);

    [PreserveSig]
    int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

    [PreserveSig]
    int GetResult(out IShellItem ppsi);

    [PreserveSig]
    int AddPlace(IShellItem psi, uint fdap);

    [PreserveSig]
    int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

    [PreserveSig]
    int Close(int hr);

    [PreserveSig]
    int SetClientGuid([MarshalAs(UnmanagedType.LPStruct)] Guid guid);

    [PreserveSig]
    int ClearClientData();

    [PreserveSig]
    int SetFilter(IntPtr pFilter);
}

[ComImport, Guid("d57c7288-d4ad-4768-be02-9d9695322fa0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IFileOpenDialog : IFileDialog
{
    [PreserveSig]
    int GetResults(out IShellItemArray ppenum);

    [PreserveSig]
    int GetSelectedItems(out IShellItemArray ppsai);
}

public static class FileDialogCLSID
{
    public static readonly Guid CLSID_FileOpenDialog = new Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
}

public static class FileDialogIID
{
    public static readonly Guid IID_IFileOpenDialog = new Guid("d57c7288-d4ad-4768-be02-9d9695322fa0");
    public static readonly Guid IID_IShellItem = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");
}
