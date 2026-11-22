using System.Text;

namespace PhotinoEx.Core.Platform.Windows.FileDragDrop;

internal static class WinFileDropPayload
{
    public const int HeaderSize = 20;

    public static byte[] Create(IReadOnlyList<string> paths)
    {
        var fileList = Encoding.Unicode.GetBytes(string.Join('\0', paths) + "\0\0");
        var payload = new byte[HeaderSize + fileList.Length];
        BitConverter.GetBytes(HeaderSize).CopyTo(payload, 0);
        BitConverter.GetBytes(1).CopyTo(payload, 16);
        fileList.CopyTo(payload, HeaderSize);
        return payload;
    }
}
