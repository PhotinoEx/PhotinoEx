using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Models;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct ComDlgFilterSpec
{
    [MarshalAs(UnmanagedType.LPWStr)] public string pszName;
    [MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
}
