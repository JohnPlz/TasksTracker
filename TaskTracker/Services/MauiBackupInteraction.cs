using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Storage;

namespace TaskTracker.Services;

public sealed class MauiBackupInteraction : IBackupInteraction
{
    public async Task<BackupSaveResult> SaveBackupAsync(string fileName, Stream stream, CancellationToken cancellationToken)
    {
        var result = await FileSaver.Default.SaveAsync(fileName, stream, cancellationToken);
        return new BackupSaveResult(result.IsSuccessful);
    }

    public async Task<BackupImportFile?> PickBackupToImportAsync(CancellationToken cancellationToken)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select backup file",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "application/octet-stream", "application/x-sqlite3", "application/db" } },
                { DevicePlatform.iOS, new[] { "public.data" } },
                { DevicePlatform.MacCatalyst, new[] { "public.data" } },
                { DevicePlatform.WinUI, new[] { ".db" } }
            })
        });

        if (result is null)
        {
            return null;
        }

        return new BackupImportFile(result.FileName, _ => result.OpenReadAsync());
    }
}