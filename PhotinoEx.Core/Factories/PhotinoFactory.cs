using System.Runtime.InteropServices;
using PhotinoEx.Core.Platform.Apple;
using PhotinoEx.Core.Platform.Linux;
using PhotinoEx.Core.Platform.Windows;

namespace PhotinoEx.Core.Factories;

public static class PhotinoFactory
{
    public static Photino Create(PhotinoInitParams initParams)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LPhotino(initParams);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WPhotino(initParams);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new APhotino(initParams);
        }

        throw new NotSupportedException("Unsupported platform");
    }
}
