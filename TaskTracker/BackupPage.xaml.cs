using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Storage;
using TaskTracker.Services;

namespace TaskTracker;

public partial class BackupPage : ContentPage
{
    private readonly TaskRepository _repository;

    public BackupPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _repository = services?.GetService<TaskRepository>() ?? new TaskRepository();

        BindingContext = this;
    }

    private async void OnBackupDatabaseClicked(object? sender, EventArgs e)
    {
        try
        {
            var fileName = $"tasktracker-backup-{DateTime.Now:yyyyMMdd-HHmm}.db";
            await using var stream = new FileStream(_repository.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);
            if (result.IsSuccessful)
            {
                await Toast.Make("Backup saved").Show();
            }
            else
            {
                await Toast.Make("Backup canceled").Show();
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Backup failed: {ex.Message}").Show();
        }
    }

    private async void OnImportBackupClicked(object? sender, EventArgs e)
    {
        try
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
                await Toast.Make("Import canceled").Show();
                return;
            }

            await using var sourceStream = await result.OpenReadAsync();
            var targetPath = _repository.DatabasePath;
            var tempPath = targetPath + ".tmp";

            await using (var tempStream = File.Create(tempPath))
            {
                await sourceStream.CopyToAsync(tempStream);
            }

            File.Copy(tempPath, targetPath, true);
            File.Delete(tempPath);

            await Toast.Make("Backup imported").Show();
        }
        catch (Exception ex)
        {
            await Toast.Make($"Import failed: {ex.Message}").Show();
        }
    }
}
