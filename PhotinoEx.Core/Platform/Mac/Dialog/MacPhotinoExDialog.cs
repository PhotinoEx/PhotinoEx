using System.Runtime.Versioning;
using PhotinoEx.Core.Models;

namespace PhotinoEx.Core.Platform.Mac.Dialog;

[SupportedOSPlatform("macos")]
internal sealed class MacPhotinoExDialog(PhotinoExWindow window) : IPhotinoExDialog
{
    public async Task<List<string>> ShowOpenFileAsync(
        string title,
        string? path,
        bool multiSelect,
        List<FileFilter>? filterPatterns
    ) => await window.ShowOpenFileDialogAsync(title, path, multiSelect, filterPatterns) ?? [];

    public async Task<List<string>> ShowOpenFolderAsync(string title, string? path, bool multiSelect) =>
        await window.ShowOpenFolderDialogAsync(title, path, multiSelect) ?? [];

    public async Task<string> ShowSaveFileAsync(
        string title,
        string? path,
        List<FileFilter>? filterPatterns,
        string defaultExtension = "txt",
        string defaultFileName = "PhotinoExFile"
    )
    {
        var defaultPath = string.IsNullOrWhiteSpace(path) ? defaultFileName : Path.Combine(path, defaultFileName);
        return await window.ShowSaveFileDialogAsync(title, defaultPath, filterPatterns) ?? "";
    }

    public Task<DialogResult> ShowMessageAsync(string title, string text, DialogButtons buttons, DialogIcon icon) =>
        window.ShowMessageDialogAsync(title, text, buttons, icon);
}
