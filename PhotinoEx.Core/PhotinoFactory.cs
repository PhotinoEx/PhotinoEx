using System.Runtime.InteropServices;
using PhotinoEx.Core.Models;
using PhotinoEx.Core.Platform;
using PhotinoEx.Core.Platform.Linux;
using PhotinoEx.Core.Platform.MacOS;
using PhotinoEx.Core.Platform.Windows;

namespace PhotinoEx.Core;

internal static class PhotinoFactory
{
    public static Photino Create(PhotinoInitParams initParams)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxPhotino(initParams);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsPhotino(initParams);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacOSPhotino(initParams);
        }

        throw new NotSupportedException("Unsupported platform");
    }
}
