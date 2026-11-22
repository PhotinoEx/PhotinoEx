using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Platform.Windows.FileDragDrop;

[ComVisible(true)]
[Guid("00000121-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IWinDropSource
{
    [PreserveSig]
    int QueryContinueDrag([MarshalAs(UnmanagedType.Bool)] bool escapePressed, uint keyState);

    [PreserveSig]
    int GiveFeedback(uint effect);
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class WinDropSource(CancellationToken cancellationToken) : IWinDropSource
{
    private const uint LeftButton = 0x0001;
    private const int DragDropSCancel = 0x00040101;
    private const int DragDropSDrop = 0x00040100;
    private const int DragDropUseDefaultCursors = 0x00040102;

    public int QueryContinueDrag(bool escapePressed, uint keyState)
    {
        if (escapePressed || cancellationToken.IsCancellationRequested)
        {
            return DragDropSCancel;
        }

        return (keyState & LeftButton) == 0 ? DragDropSDrop : 0;
    }

    public int GiveFeedback(uint effect) => DragDropUseDefaultCursors;
}
