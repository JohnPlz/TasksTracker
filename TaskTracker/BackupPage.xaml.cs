using CommunityToolkit.Maui.Alerts;
using TaskTracker.Services;

namespace TaskTracker;

public partial class BackupPage : ContentPage
{
    private readonly BackupService _backupService;

    public BackupPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _backupService = services?.GetService<BackupService>()
            ?? new BackupService(
                new TaskRepository().DatabasePath,
                new SystemBackupFileSystem(),
                new SystemBackupClock(),
                new MauiBackupInteraction());

        BindingContext = this;
    }

    private async void OnBackupDatabaseClicked(object? sender, EventArgs e)
    {
        var result = await _backupService.CreateBackupAsync();
        await Toast.Make(result.Message).Show();
    }

    private async void OnImportBackupClicked(object? sender, EventArgs e)
    {
        var result = await _backupService.ImportBackupAsync();
        await Toast.Make(result.Message).Show();
    }
}
