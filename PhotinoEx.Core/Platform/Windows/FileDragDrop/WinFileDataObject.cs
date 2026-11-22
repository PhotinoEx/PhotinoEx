using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using PhotinoEx.Core.Platform.Windows.Utils;

namespace PhotinoEx.Core.Platform.Windows.FileDragDrop;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class WinFileDataObject(IReadOnlyList<string> paths) : IDataObject
{
    private const short CfHDrop = 15;
    private const int DvEFormatEtc = unchecked((int)0x80040064);
    private static readonly FORMATETC Format = new()
    {
        cfFormat = CfHDrop,
        dwAspect = DVASPECT.DVASPECT_CONTENT,
        lindex = -1,
        tymed = TYMED.TYMED_HGLOBAL,
    };

    public void GetData(ref FORMATETC format, out STGMEDIUM medium)
    {
        if (QueryGetData(ref format) != 0)
        {
            throw new COMException("The requested drag format is unavailable.", DvEFormatEtc);
        }

        var payload = WinFileDropPayload.Create(paths);
        var memory = WinApi.GlobalAlloc(0x0002, (UIntPtr)payload.Length);
        if (memory == IntPtr.Zero)
        {
            throw new OutOfMemoryException();
        }

        var target = WinApi.GlobalLock(memory);
        if (target == IntPtr.Zero)
        {
            WinApi.GlobalFree(memory);
            throw new COMException("Unable to lock drag data.", Marshal.GetHRForLastWin32Error());
        }

        try
        {
            try
            {
                Marshal.Copy(payload, 0, target, payload.Length);
            }
            finally
            {
                WinApi.GlobalUnlock(memory);
            }
        }
        catch
        {
            WinApi.GlobalFree(memory);
            throw;
        }

        medium = new STGMEDIUM
        {
            tymed = TYMED.TYMED_HGLOBAL,
            unionmember = memory,
            pUnkForRelease = null,
        };
    }

    public int QueryGetData(ref FORMATETC format) =>
        format.cfFormat == CfHDrop
        && (format.tymed & TYMED.TYMED_HGLOBAL) != 0
        && format.dwAspect == DVASPECT.DVASPECT_CONTENT
            ? 0
            : DvEFormatEtc;

    public IEnumFORMATETC EnumFormatEtc(DATADIR direction) => direction == DATADIR.DATADIR_GET
        ? new FormatEnumerator([Format])
        : throw new COMException("Setting drag data is unsupported.", ENotImpl);

    public void GetDataHere(ref FORMATETC format, ref STGMEDIUM medium) =>
        throw new COMException("Caller-provided storage is unsupported.", ENotImpl);

    public int GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
    {
        formatOut = formatIn;
        formatOut.ptd = IntPtr.Zero;
        return DataSFormatEtc;
    }

    public void SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release) =>
        throw new COMException("Setting drag data is unsupported.", ENotImpl);

    public int DAdvise(ref FORMATETC format, ADVF advf, IAdviseSink adviseSink, out int connection)
    {
        connection = 0;
        return OLEAdviseNotSupported;
    }

    public void DUnadvise(int connection) =>
        throw new COMException("Data advisory connections are unsupported.", OLEAdviseNotSupported);

    public int EnumDAdvise(out IEnumSTATDATA? enumAdvise)
    {
        enumAdvise = null;
        return OLEAdviseNotSupported;
    }

    private const int ENotImpl = unchecked((int)0x80004001);
    private const int DataSFormatEtc = 0x00040130;
    private const int OLEAdviseNotSupported = unchecked((int)0x80040003);

    private sealed class FormatEnumerator(FORMATETC[] formats) : IEnumFORMATETC
    {
        private int _index;

        public int Next(int count, FORMATETC[] elements, int[]? fetched)
        {
            var copied = 0;
            while (copied < count && _index < formats.Length)
            {
                elements[copied++] = formats[_index++];
            }

            if (fetched is { Length: > 0 })
            {
                fetched[0] = copied;
            }

            return copied == count ? 0 : 1;
        }

        public int Skip(int count)
        {
            var skipped = Math.Min(count, formats.Length - _index);
            _index += skipped;
            return skipped == count ? 0 : 1;
        }

        public int Reset()
        {
            _index = 0;
            return 0;
        }

        public void Clone(out IEnumFORMATETC newEnum)
        {
            var clone = new FormatEnumerator(formats) { _index = _index };
            newEnum = clone;
        }
    }
}
