using PhotinoEx.Core.Enums;
using PhotinoEx.Core.Models;

namespace PhotinoEx.Core.Platform;

public interface IDialog
{
    // Tested - linux / windows
    // untested - apple
    public Task<List<string>> ShowOpenFileAsync(string title, string? path, bool multiSelect, List<FileFilter>? filterPatterns);

    // Tested - linux / windows
    // untested - apple
    public Task<List<string>> ShowOpenFolderAsync(string title, string? path, bool multiSelect);

    // Tested - linux / windows
    // untested - apple
    public Task<string> ShowSaveFileAsync(string title, string? path, List<FileFilter>? filterPatterns, string defaultExtension = "txt", string defaultFileName = "PhotinoExFile");

    // Tested - linux / windows
    // Untested - apple
    public Task<DialogResult> ShowMessageAsync(string title, string text, DialogButtons buttons, DialogIcon icon);
}
