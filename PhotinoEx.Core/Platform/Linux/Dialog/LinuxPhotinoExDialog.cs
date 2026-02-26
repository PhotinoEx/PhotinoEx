using Gtk;
using PhotinoEx.Core.Models;
using FileFilter = Gtk.FileFilter;
using PhotinoExFileFilter = PhotinoEx.Core.Models.FileFilter;
using File = Gio.File;
using FileDialog = Gtk.FileDialog;
using MessageDialog = Gtk.MessageDialog;

namespace PhotinoEx.Core.Platform.Linux.Dialog;

public class LinuxPhotinoExDialog : IPhotinoExDialog
{
    private Window _window { get; set; }

    public LinuxPhotinoExDialog(Window window)
    {
        _window = window;
    }

    public async Task<List<string>> ShowOpenFileAsync(string title, string? path, bool multiSelect,
        List<PhotinoExFileFilter>? filterPatterns)
    {
        var dialog = FileDialog.New();
        dialog.SetTitle(title);

        var filter = FileFilter.New();
        filter.Name = "FilterPatterns";
        foreach (var s in filterPatterns ?? new List<PhotinoExFileFilter>())
        {
            filter.AddPattern(s.Spec); // *.txt
        }

        var results = new List<string>();

        try
        {
            if (multiSelect)
            {
                var files = await dialog.OpenMultipleAsync(_window);

                for (uint i = 0; i < files?.GetNItems(); i++)
                {
                    var item = files.GetObject(i) as File;
                    if (item is null)
                    {
                        return results;
                    }

                    results.Add(item.GetPath()!);
                }
            }
            else
            {
                var file = await dialog.OpenAsync(_window);

                if (file is not null)
                {
                    var pathToUse = file.GetPath();
                    results.Add(pathToUse!);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return results;
    }

    /// <summary>
    /// TODO: GTK4 currently does not allow multiselect on folders. the api is there, just does not let me multiselect
    /// </summary>
    /// <param name="title"></param>
    /// <param name="path"></param>
    /// <param name="multiSelect"></param>
    /// <returns></returns>
    public async Task<List<string>> ShowOpenFolderAsync(string title, string? path, bool multiSelect)
    {
        var dialog = FileDialog.New();
        dialog.SetTitle(title);

        var results = new List<string>();

        try
        {
            if (multiSelect)
            {
                var files = await dialog.SelectMultipleFoldersAsync(_window);

                for (uint i = 0; i < files?.GetNItems(); i++)
                {
                    var item = files.GetObject(i) as File;
                    if (item is null)
                    {
                        return results;
                    }

                    results.Add(item.GetPath()!);
                }
            }
            else
            {
                var file = await dialog.SelectFolderAsync(_window);

                if (file is not null)
                {
                    var pathToUse = file.GetPath();
                    results.Add(pathToUse!);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return results;
    }

    public async Task<string> ShowSaveFileAsync(string title, string? path, List<PhotinoExFileFilter>? filterPatterns,
        string defaultExtension = "txt",
        string defaultFileName = "PhotinoExFile")
    {
        var dialog = FileDialog.New();
        dialog.SetTitle(title);

        var filter = FileFilter.New();
        filter.Name = "FilterPatterns";
        foreach (var filters in filterPatterns ?? new List<PhotinoExFileFilter>())
        {
            filter.AddPattern(filters.Spec); // *.txt
        }

        File? file = null;
        try
        {
            file = await dialog.SaveAsync(_window);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (file is null)
        {
            return "";
        }

        return file.GetPath()!;
    }

    public async Task<DialogResult> ShowMessageAsync(string title, string text, DialogButtons buttons, DialogIcon icon)
    {
        var dialog = new MessageDialog();
        dialog.SetTitle(title);
        dialog.Text = text;
        dialog.SetModal(true);
        dialog.SetTransientFor(_window);

        switch (buttons)
        {
            case DialogButtons.Ok:
                {
                    dialog.AddButton("Ok", (int) DialogResult.Ok);
                    break;
                }
            case DialogButtons.OkCancel:
                {
                    dialog.AddButton("Ok", (int) DialogResult.Ok);
                    dialog.AddButton("Cancel", (int) DialogResult.Cancel);
                    break;
                }
            case DialogButtons.YesNo:
                {
                    dialog.AddButton("Yes", (int) DialogResult.Yes);
                    dialog.AddButton("No", (int) DialogResult.No);
                    break;
                }
            case DialogButtons.YesNoCancel:
                {
                    dialog.AddButton("Yes", (int) DialogResult.Yes);
                    dialog.AddButton("No", (int) DialogResult.No);
                    dialog.AddButton("Cancel", (int) DialogResult.Cancel);
                    break;
                }
            case DialogButtons.RetryCancel:
                {
                    dialog.AddButton("Retry", (int) DialogResult.Retry);
                    dialog.AddButton("Cancel", (int) DialogResult.Cancel);
                    break;
                }
            case DialogButtons.AbortRetryIgnore:
                {
                    dialog.AddButton("Abort", (int) DialogResult.Abort);
                    dialog.AddButton("Retry", (int) DialogResult.Retry);
                    dialog.AddButton("Cancel", (int) DialogResult.Cancel);
                    break;
                }
            default:
                dialog.AddButton("Ok", (int) DialogResult.Ok);
                break;
        }

        var tcs = new TaskCompletionSource<DialogResult>();

        dialog.OnResponse += (_, args) =>
        {
            switch (args.ResponseId)
            {
                case (int) ResponseType.Close:
                    tcs.SetResult(DialogResult.Cancel);
                    break;
                case (int) DialogResult.Ok:
                    tcs.SetResult(DialogResult.Ok);
                    break;
                case (int) DialogResult.Yes:
                    tcs.SetResult(DialogResult.Yes);
                    break;
                case (int) DialogResult.No:
                    tcs.SetResult(DialogResult.No);
                    break;
                case (int) DialogResult.Cancel:
                    tcs.SetResult(DialogResult.Cancel);
                    break;
                case (int) DialogResult.Abort:
                    tcs.SetResult(DialogResult.Abort);
                    break;
                case (int) DialogResult.Retry:
                    tcs.SetResult(DialogResult.Retry);
                    break;
                case (int) DialogResult.Ignore:
                    tcs.SetResult(DialogResult.Ignore);
                    break;
                default:
                    tcs.SetResult(DialogResult.Cancel);
                    break;
            }

            dialog.Destroy();
        };

        dialog.Present();

        return await tcs.Task;
    }
}
