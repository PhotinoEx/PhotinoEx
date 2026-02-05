using System.Runtime.InteropServices;
using PhotinoEx.Core.Platform.Apple;
using PhotinoEx.Core.Platform.Linux;
using PhotinoEx.Core.Platform.Windows;

namespace PhotinoEx.Core.Factories;

public static class PhotinoDialogFactory
{
    public static PhotinoDialog CreateForWindows(Photino window)
    {
        return new WPhotinoDialog(window);
    }

    public static PhotinoDialog Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LPhotinoDialog();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new APhotinoDialog();
        }

        throw new PlatformNotSupportedException("Platform not supported");
    }
}
