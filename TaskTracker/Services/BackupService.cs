namespace TaskTracker.Services;

public sealed class BackupService
{
    private readonly string _databasePath;
    private readonly IBackupFileSystem _fileSystem;
    private readonly IBackupClock _clock;
    private readonly IBackupInteraction _interaction;

    public BackupService(
        string databasePath,
        IBackupFileSystem fileSystem,
        IBackupClock clock,
        IBackupInteraction interaction)
    {
        _databasePath = databasePath;
        _fileSystem = fileSystem;
        _clock = clock;
        _interaction = interaction;
    }

    public async Task<BackupOperationResult> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = $"tasktracker-backup-{_clock.Now:yyyyMMdd-HHmm}.db";
            await using var stream = _fileSystem.OpenRead(_databasePath);
            var result = await _interaction.SaveBackupAsync(fileName, stream, cancellationToken);

            return result.IsSuccessful
                ? BackupOperationResult.Success("Backup saved")
                : BackupOperationResult.Canceled("Backup canceled");
        }
        catch (Exception ex)
        {
            return BackupOperationResult.Failure($"Backup failed: {ex.Message}");
        }
    }

    public async Task<BackupOperationResult> ImportBackupAsync(CancellationToken cancellationToken = default)
    {
        var tempPath = _databasePath + ".tmp";

        try
        {
            var file = await _interaction.PickBackupToImportAsync(cancellationToken);
            if (file is null)
            {
                return BackupOperationResult.Canceled("Import canceled");
            }

            await using var sourceStream = await file.OpenReadAsync(cancellationToken);
            await using (var tempStream = _fileSystem.Create(tempPath))
            {
                await sourceStream.CopyToAsync(tempStream, cancellationToken);
            }

            _fileSystem.Copy(tempPath, _databasePath, true);
            _fileSystem.Delete(tempPath);

            return BackupOperationResult.Success("Backup imported");
        }
        catch (Exception ex)
        {
            TryDeleteTempFile(tempPath);
            return BackupOperationResult.Failure($"Import failed: {ex.Message}");
        }
    }

    private void TryDeleteTempFile(string tempPath)
    {
        if (!_fileSystem.Exists(tempPath))
        {
            return;
        }

        try
        {
            _fileSystem.Delete(tempPath);
        }
        catch
        {
        }
    }
}

public sealed record BackupOperationResult(bool IsSuccessful, bool IsCanceled, string Message)
{
    public static BackupOperationResult Success(string message) => new(true, false, message);

    public static BackupOperationResult Canceled(string message) => new(false, true, message);

    public static BackupOperationResult Failure(string message) => new(false, false, message);
}

public sealed record BackupSaveResult(bool IsSuccessful);

public sealed class BackupImportFile
{
    private readonly Func<CancellationToken, Task<Stream>> _openReadAsync;

    public BackupImportFile(string fileName, Func<CancellationToken, Task<Stream>> openReadAsync)
    {
        FileName = fileName;
        _openReadAsync = openReadAsync;
    }

    public string FileName { get; }

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
    {
        return _openReadAsync(cancellationToken);
    }
}

public interface IBackupInteraction
{
    Task<BackupSaveResult> SaveBackupAsync(string fileName, Stream stream, CancellationToken cancellationToken);

    Task<BackupImportFile?> PickBackupToImportAsync(CancellationToken cancellationToken);
}

public interface IBackupFileSystem
{
    Stream OpenRead(string path);

    Stream Create(string path);

    void Copy(string sourceFileName, string destFileName, bool overwrite);

    void Delete(string path);

    bool Exists(string path);
}

public sealed class SystemBackupFileSystem : IBackupFileSystem
{
    public Stream OpenRead(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public Stream Create(string path)
    {
        return File.Create(path);
    }

    public void Copy(string sourceFileName, string destFileName, bool overwrite)
    {
        File.Copy(sourceFileName, destFileName, overwrite);
    }

    public void Delete(string path)
    {
        File.Delete(path);
    }

    public bool Exists(string path)
    {
        return File.Exists(path);
    }
}

public interface IBackupClock
{
    DateTime Now { get; }
}

public sealed class SystemBackupClock : IBackupClock
{
    public DateTime Now => DateTime.Now;
}