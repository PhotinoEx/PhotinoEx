using System.Runtime.InteropServices;
using Gdk;
using Gio;
using Gtk;
using PhotinoEx.Core.Models;

namespace PhotinoEx.Core.Platform.Linux;

internal sealed class LinFileDragDrop
{
    private readonly DragSource _source = DragSource.New();
    private readonly DropTarget _target;
    private readonly Action<FilesDroppedEventArgs> _filesDropped;
    private readonly SynchronizationContext _synchronizationContext;
    private ContentProvider? _content;
    private FileList? _fileList;
    private Gio.File[]? _files;
    private TaskCompletionSource<FileDragDropEffects>? _completion;
    private CancellationTokenRegistration _cancellationRegistration;

    public LinFileDragDrop(
        Widget widget,
        Action<FilesDroppedEventArgs> filesDropped,
        SynchronizationContext synchronizationContext
    )
    {
        _filesDropped = filesDropped;
        _synchronizationContext = synchronizationContext;

        _source.OnDragEnd += OnDragEnd;
        _source.OnDragCancel += OnDragCancel;
        widget.AddController(_source);

        _target = DropTarget.New(
            FileList.GetGType(),
            DragAction.Copy | DragAction.Move | DragAction.Link
        );
        _target.OnDrop += OnDrop;
        widget.AddController(_target);
    }

    public Task<FileDragDropEffects> BeginAsync(
        IReadOnlyList<string> paths,
        FileDragDropEffects allowedEffects,
        CancellationToken cancellationToken
    )
    {
        if (_completion is not null)
        {
            throw new InvalidOperationException("A file drag operation is already active.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        _files = paths.Select(FileHelper.NewForPath).ToArray();
        _fileList = FileList.NewFromArray(_files, (nuint) _files.Length);
        var value = new GObject.Value(FileList.GetGType());
        value.SetBoxed(((GLib.BoxedRecord) _fileList).GetHandle());
        _content = ContentProvider.NewForValue(value);
        _source.SetContent(_content);
        _source.SetActions(ToGdk(allowedEffects));

        _completion = new TaskCompletionSource<FileDragDropEffects>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _cancellationRegistration = cancellationToken.Register(() =>
            _synchronizationContext.Post(_ => _source.DragCancel(), null)
        );
        return _completion.Task;
    }

    private void OnDragEnd(DragSource sender, DragSource.DragEndSignalArgs args) =>
        Complete(FromGdk(args.Drag.GetSelectedAction()));

    private bool OnDragCancel(DragSource sender, DragSource.DragCancelSignalArgs args)
    {
        Complete(FileDragDropEffects.None);
        return false;
    }

    private bool OnDrop(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        var paths = GetPaths(args.Value.GetBoxed());
        if (paths.Count == 0)
        {
            return false;
        }

        var actions = sender.GetCurrentDrop()?.GetActions() ?? DragAction.Copy;
        _filesDropped(
            new FilesDroppedEventArgs(
                paths,
                FromGdk(PreferredAction(actions)),
                (int) args.X,
                (int) args.Y
            )
        );
        return true;
    }

    private void Complete(FileDragDropEffects effect)
    {
        var completion = _completion;
        if (completion is null)
        {
            return;
        }

        _completion = null;
        _cancellationRegistration.Dispose();
        _source.SetContent(null!);
        _content = null;
        _fileList = null;
        _files = null;
        completion.TrySetResult(effect);
    }

    internal static IReadOnlyList<string> GetPaths(IntPtr fileList)
    {
        var paths = new List<string>();
        var node = gdk_file_list_get_files(fileList);
        while (node != IntPtr.Zero)
        {
            var list = Marshal.PtrToStructure<GSList>(node);
            var path = g_file_get_path(list.Data);
            if (path != IntPtr.Zero)
            {
                try
                {
                    paths.Add(Marshal.PtrToStringUTF8(path)!);
                }
                finally
                {
                    g_free(path);
                }
            }

            node = list.Next;
        }

        return paths;
    }

    internal static DragAction ToGdk(FileDragDropEffects effects) => (DragAction) (uint) effects;

    internal static FileDragDropEffects FromGdk(DragAction action) =>
        (FileDragDropEffects) (uint) action;

    private static DragAction PreferredAction(DragAction actions)
    {
        if ((actions & DragAction.Copy) != 0)
        {
            return DragAction.Copy;
        }

        if ((actions & DragAction.Move) != 0)
        {
            return DragAction.Move;
        }

        return (actions & DragAction.Link) != 0 ? DragAction.Link : DragAction.None;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GSList
    {
        public readonly IntPtr Data;
        public readonly IntPtr Next;
    }

    [DllImport("libgtk-4.so.1")]
    private static extern IntPtr gdk_file_list_get_files(IntPtr fileList);

    [DllImport("libgio-2.0.so.0")]
    private static extern IntPtr g_file_get_path(IntPtr file);

    [DllImport("libglib-2.0.so.0")]
    private static extern void g_free(IntPtr memory);
}
