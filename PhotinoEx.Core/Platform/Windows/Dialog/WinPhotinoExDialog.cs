using System.Runtime.InteropServices;
using PhotinoEx.Core.Models;
using PhotinoEx.Core.Utils;

namespace PhotinoEx.Core.Platform.Windows.Dialog;

public class WinPhotinoExDialog : IPhotinoExDialog
{
    private IntPtr _hwnd { get; set; }

    public WinPhotinoExDialog(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public async Task<List<string>> ShowOpenFileAsync(string title, string? path, bool multiSelect, List<FileFilter>? filterPatterns)
    {
        var dialog = (IFileOpenDialog) Activator.CreateInstance(Type.GetTypeFromCLSID(WinConstants.CLSID_FileOpenDialog));
        var result = new List<string>();

        try
        {
            dialog!.GetOptions(out uint options);
            options |= WinConstants.FOS_FILEMUSTEXIST | WinConstants.FOS_FORCEFILESYSTEM | WinConstants.FOS_PATHMUSTEXIST;
            if (multiSelect)
            {
                options |= WinConstants.FOS_ALLOWMULTISELECT;
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
                if (WinAPi.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out IShellItem folder) == WinConstants.S_OK)
                {
                    dialog.SetFolder(folder);
                }
            }

            var hr = dialog.Show(_hwnd);

            if (hr == WinConstants.ERROR_CANCELLED)
            {
                return result;
            }

            if (hr != WinConstants.S_OK)
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
        var dialog = (IFileOpenDialog) Activator.CreateInstance(Type.GetTypeFromCLSID(WinConstants.CLSID_FileOpenDialog));
        var result = new List<string>();

        try
        {
            dialog!.GetOptions(out uint options);
            options |= WinConstants.FOS_PICKFOLDERS | WinConstants.FOS_FORCEFILESYSTEM | WinConstants.FOS_PATHMUSTEXIST;
            if (multiSelect)
            {
                options |= WinConstants.FOS_ALLOWMULTISELECT;
            }
            dialog.SetOptions(options);
            dialog.SetTitle(title);
            dialog.SetOkButtonLabel("Select");

            if (!string.IsNullOrEmpty(path))
            {
                var iid = typeof(IShellItem).GUID;
                if (WinAPi.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out IShellItem folder) == WinConstants.S_OK)
                {
                    dialog.SetFolder(folder);
                }
            }

            var hr = dialog.Show(_hwnd);

            if (hr == WinConstants.ERROR_CANCELLED)
            {
                return result;
            }

            if (hr != WinConstants.S_OK)
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
        var dialog = (IFileSaveDialog) Activator.CreateInstance(Type.GetTypeFromCLSID(WinConstants.CLSID_FileSaveDialog));

        try
        {
            dialog!.GetOptions(out uint options);
            options |= WinConstants.FOS_FORCEFILESYSTEM | WinConstants.FOS_PATHMUSTEXIST | WinConstants.FOS_OVERWRITEPROMPT;
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
                if (WinAPi.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out IShellItem startFolder) == WinConstants.S_OK)
                {
                    dialog.SetFolder(startFolder);
                }
            }

            int hr = dialog.Show(_hwnd);

            if (hr == WinConstants.ERROR_CANCELLED)
            {
                return "";
            }

            if (hr != WinConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(WinConstants.SIGDN_FILESYSPATH, out string pathToUse);
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
                flags |= WinConstants.MB_ICONINFORMATION;
                break;
            case DialogIcon.Warning:
                flags |= WinConstants.MB_ICONWARNING;
                break;
            case DialogIcon.Error:
                flags |= WinConstants.MB_ICONERROR;
                break;
            case DialogIcon.Question:
                flags |= WinConstants.MB_ICONQUESTION;
                break;
        }

        switch (buttons)
        {
            case DialogButtons.Ok:
                flags |= WinConstants.MB_OK;
                break;
            case DialogButtons.OkCancel:
                flags |= WinConstants.MB_OKCANCEL;
                break;
            case DialogButtons.YesNo:
                flags |= WinConstants.MB_YESNO;
                break;
            case DialogButtons.YesNoCancel:
                flags |= WinConstants.MB_YESNOCANCEL;
                break;
            case DialogButtons.RetryCancel:
                flags |= WinConstants.MB_RETRYCANCEL;
                break;
            case DialogButtons.AbortRetryIgnore:
                flags |= WinConstants.MB_ABORTRETRYIGNORE;
                break;
        }

        int result = WinAPi.MessageBoxW(_hwnd, text, title, flags);

        switch (result)
        {
            case WinConstants.IDOK:
                return DialogResult.Ok;
            case WinConstants.IDCANCEL:
                return DialogResult.Cancel;
            case WinConstants.IDYES:
                return DialogResult.Yes;
            case WinConstants.IDNO:
                return DialogResult.No;
            case WinConstants.IDABORT:
                return DialogResult.Abort;
            case WinConstants.IDRETRY:
                return DialogResult.Retry;
            case WinConstants.IDIGNORE:
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
                item.GetDisplayName(WinConstants.SIGDN_FILESYSPATH, out string pathToUse);
                result.Add(pathToUse);
            }

            return result;
        }
        else
        {
            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(WinConstants.SIGDN_FILESYSPATH, out string pathToUse);
            result.Add(pathToUse);
            return result;
        }
    }
}
