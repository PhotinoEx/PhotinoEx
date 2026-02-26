using System.Runtime.InteropServices;
using PhotinoEx.Core.Models;
using PhotinoEx.Core.Utils;

namespace PhotinoEx.Core.Platform.Windows.Dialog;

public class WindowsDialog : IDialog
{
    private IntPtr _hwnd { get; set; }

    public WindowsDialog(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public async Task<List<string>> ShowOpenFileAsync(string title, string? path, bool multiSelect, List<FileFilter>? filterPatterns)
    {
        var dialog = (IFileOpenDialog) Activator.CreateInstance(Type.GetTypeFromCLSID(Constants.CLSID_FileOpenDialog));
        var result = new List<string>();

        try
        {
            dialog!.GetOptions(out uint options);
            options |= Constants.FOS_FILEMUSTEXIST | Constants.FOS_FORCEFILESYSTEM | Constants.FOS_PATHMUSTEXIST;
            if (multiSelect)
            {
                options |= Constants.FOS_ALLOWMULTISELECT;
            }
            dialog.SetOptions(options);
            dialog.SetTitle(title);
            dialog.SetOkButtonLabel("Select");

            filterPatterns ??= new List<FileFilter>()
            {
                new FileFilter("All Files", "*.*")
            };

            var specs = filterPatterns.Select(f => new ComDlgFilterSpec()
            {
                pszName = f.Name,
                pszSpec = f.Spec
            }).ToArray();

            dialog.SetFileTypes((uint) specs.Length, specs);
            dialog.SetFileTypeIndex(1);

            if (!string.IsNullOrEmpty(path))
            {
                var iid = typeof(IShellItem).GUID;
                if (DLLImports.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out IShellItem folder) == Constants.S_OK)
                {
                    dialog.SetFolder(folder);
                }
            }

            var hr = dialog.Show(_hwnd);

            if (hr == Constants.ERROR_CANCELLED)
            {
                return result;
            }

            if (hr != Constants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return GetResults(dialog, multiSelect);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    public async Task<List<string>> ShowOpenFolderAsync(string title, string? path, bool multiSelect)
    {
        var dialog = (IFileOpenDialog) Activator.CreateInstance(Type.GetTypeFromCLSID(Constants.CLSID_FileOpenDialog));
        var result = new List<string>();

        try
        {
            dialog!.GetOptions(out uint options);
            options |= Constants.FOS_PICKFOLDERS | Constants.FOS_FORCEFILESYSTEM | Constants.FOS_PATHMUSTEXIST;
            if (multiSelect)
            {
                options |= Constants.FOS_ALLOWMULTISELECT;
            }
            dialog.SetOptions(options);
            dialog.SetTitle(title);
            dialog.SetOkButtonLabel("Select");

            if (!string.IsNullOrEmpty(path))
            {
                var iid = typeof(IShellItem).GUID;
                if (DLLImports.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out IShellItem folder) == Constants.S_OK)
                {
                    dialog.SetFolder(folder);
                }
            }

            var hr = dialog.Show(_hwnd);

            if (hr == Constants.ERROR_CANCELLED)
            {
                return result;
            }

            if (hr != Constants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return GetResults(dialog, multiSelect);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    public async Task<string> ShowSaveFileAsync(string title, string? path, List<FileFilter>? filterPatterns, string defaultExtension = "txt",
        string defaultFileName = "PhotinoExFile")
    {
        var dialog = (IFileSaveDialog) Activator.CreateInstance(Type.GetTypeFromCLSID(Constants.CLSID_FileSaveDialog));

        try
        {
            dialog!.GetOptions(out uint options);
            options |= Constants.FOS_FORCEFILESYSTEM | Constants.FOS_PATHMUSTEXIST | Constants.FOS_OVERWRITEPROMPT;
            dialog.SetOptions(options);

            dialog.SetTitle(title);
            dialog.SetOkButtonLabel("Save");

            filterPatterns ??= new List<FileFilter>();
            if (!filterPatterns.Any())
            {
                filterPatterns.Add(new FileFilter("All Files", "*.*"));
            }

            var specs = filterPatterns.Select(f => new ComDlgFilterSpec()
            {
                pszName = f.Name,
                pszSpec = f.Spec
            }).ToArray();

            dialog.SetFileTypes((uint) specs.Length, specs);
            dialog.SetFileTypeIndex(1);

            if (!string.IsNullOrEmpty(defaultFileName))
            {
                dialog.SetFileName(defaultFileName);
            }

            if (!string.IsNullOrEmpty(defaultExtension))
            {
                dialog.SetDefaultExtension(defaultExtension.TrimStart('.'));
            }

            if (!string.IsNullOrEmpty(path))
            {
                var iid = typeof(IShellItem).GUID;
                if (DLLImports.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out IShellItem startFolder) == Constants.S_OK)
                {
                    dialog.SetFolder(startFolder);
                }
            }

            int hr = dialog.Show(_hwnd);

            if (hr == Constants.ERROR_CANCELLED)
            {
                return "";
            }

            if (hr != Constants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(Constants.SIGDN_FILESYSPATH, out string pathToUse);
            return pathToUse;
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    public async Task<DialogResult> ShowMessageAsync(string title, string text, DialogButtons buttons, DialogIcon icon)
    {
        uint flags = 0;

        switch (icon)
        {
            case DialogIcon.Info:
                flags |= Constants.MB_ICONINFORMATION;
                break;
            case DialogIcon.Warning:
                flags |= Constants.MB_ICONWARNING;
                break;
            case DialogIcon.Error:
                flags |= Constants.MB_ICONERROR;
                break;
            case DialogIcon.Question:
                flags |= Constants.MB_ICONQUESTION;
                break;
        }

        switch (buttons)
        {
            case DialogButtons.Ok:
                flags |= Constants.MB_OK;
                break;
            case DialogButtons.OkCancel:
                flags |= Constants.MB_OKCANCEL;
                break;
            case DialogButtons.YesNo:
                flags |= Constants.MB_YESNO;
                break;
            case DialogButtons.YesNoCancel:
                flags |= Constants.MB_YESNOCANCEL;
                break;
            case DialogButtons.RetryCancel:
                flags |= Constants.MB_RETRYCANCEL;
                break;
            case DialogButtons.AbortRetryIgnore:
                flags |= Constants.MB_ABORTRETRYIGNORE;
                break;
        }

        int result = DLLImports.MessageBoxW(_hwnd, text, title, flags);

        switch (result)
        {
            case Constants.IDOK:
                return DialogResult.Ok;
            case Constants.IDCANCEL:
                return DialogResult.Cancel;
            case Constants.IDYES:
                return DialogResult.Yes;
            case Constants.IDNO:
                return DialogResult.No;
            case Constants.IDABORT:
                return DialogResult.Abort;
            case Constants.IDRETRY:
                return DialogResult.Retry;
            case Constants.IDIGNORE:
                return DialogResult.Ignore;
            default:
                return DialogResult.Cancel;
        }
    }

    private List<string> GetResults(IFileOpenDialog dialog, bool multiSelect)
    {
        var result = new List<string>();

        if (multiSelect)
        {
            dialog.GetResults(out IShellItemArray results);
            results.GetCount(out uint count);

            for (uint i = 0; i < count; i++)
            {
                results.GetItemAt(i, out IShellItem item);
                item.GetDisplayName(Constants.SIGDN_FILESYSPATH, out string pathToUse);
                result.Add(pathToUse);
            }

            return result;
        }
        else
        {
            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(Constants.SIGDN_FILESYSPATH, out string pathToUse);
            result.Add(pathToUse);
            return result;
        }
    }
}
