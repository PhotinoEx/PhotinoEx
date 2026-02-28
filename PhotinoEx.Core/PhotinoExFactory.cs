using System.Runtime.InteropServices;
using PhotinoEx.Core.Models;
using PhotinoEx.Core.Platform;
using PhotinoEx.Core.Platform.Linux;
using PhotinoEx.Core.Platform.Mac;
using PhotinoEx.Core.Platform.Windows;

namespace PhotinoEx.Core;

internal static class PhotinoExFactory
{
    public static Platform.PhotinoEx Create(PhotinoExInitParams exInitParams)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinPhotinoEx(exInitParams);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WinPhotinoEx(exInitParams);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacPhotinoEx(exInitParams);
        }

        throw new NotSupportedException("Unsupported platform");
    }
}
