using System.Runtime.InteropServices;

namespace PhotinoEx.Core;

public static class PhotinoFactory
{
    public static Photino Create(PhotinoInitParams initParams)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new PhotinoLinux(initParams);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new PhotinoWindows(initParams);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new PhotinoApple(initParams);
        }

        throw new NotSupportedException("Unsupported platform");
    }
}
