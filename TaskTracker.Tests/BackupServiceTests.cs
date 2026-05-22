using System.Text;
using TaskTracker.Services;
using Xunit;

namespace TaskTracker.Tests;

public sealed class BackupServiceTests
{
    [Fact]
    public async Task CreateBackupAsync_UsesTimestampedFileNameAndDatabaseContent()
    {
        var databasePath = "tasktracker.db";
        var fileSystem = new FakeBackupFileSystem();
        fileSystem.SeedFile(databasePath, "database-content");

        var interaction = new FakeBackupInteraction
        {
            SaveResult = new BackupSaveResult(true)
        };

        var service = new BackupService(
            databasePath,
            fileSystem,
            new FakeBackupClock(new DateTime(2026, 5, 22, 8, 30, 0)),
            interaction);

        var result = await service.CreateBackupAsync();

        Assert.True(result.IsSuccessful);
        Assert.False(result.IsCanceled);
        Assert.Equal("Backup saved", result.Message);
        Assert.Equal("tasktracker-backup-20260522-0830.db", interaction.SavedFileName);
        Assert.Equal("database-content", interaction.SavedContent);
    }

    [Fact]
    public async Task CreateBackupAsync_WhenSaveIsCanceled_ReturnsCanceledResult()
    {
        var fileSystem = new FakeBackupFileSystem();
        fileSystem.SeedFile("tasktracker.db", "database-content");

        var service = new BackupService(
            "tasktracker.db",
            fileSystem,
            new FakeBackupClock(new DateTime(2026, 5, 22, 8, 30, 0)),
            new FakeBackupInteraction
            {
                SaveResult = new BackupSaveResult(false)
            });

        var result = await service.CreateBackupAsync();

        Assert.False(result.IsSuccessful);
        Assert.True(result.IsCanceled);
        Assert.Equal("Backup canceled", result.Message);
    }

    [Fact]
    public async Task CreateBackupAsync_WhenDatabaseReadFails_ReturnsFailureResult()
    {
        var service = new BackupService(
            "missing.db",
            new FakeBackupFileSystem(),
            new FakeBackupClock(new DateTime(2026, 5, 22, 8, 30, 0)),
            new FakeBackupInteraction());

        var result = await service.CreateBackupAsync();

        Assert.False(result.IsSuccessful);
        Assert.False(result.IsCanceled);
        Assert.StartsWith("Backup failed:", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportBackupAsync_WhenSelectionIsCanceled_ReturnsCanceledResult()
    {
        var service = new BackupService(
            "tasktracker.db",
            new FakeBackupFileSystem(),
            new FakeBackupClock(DateTime.Now),
            new FakeBackupInteraction
            {
                PickedFile = null
            });

        var result = await service.ImportBackupAsync();

        Assert.False(result.IsSuccessful);
        Assert.True(result.IsCanceled);
        Assert.Equal("Import canceled", result.Message);
    }

    [Fact]
    public async Task ImportBackupAsync_OverwritesDatabaseAndDeletesTempFile()
    {
        var databasePath = "tasktracker.db";
        var tempPath = databasePath + ".tmp";
        var fileSystem = new FakeBackupFileSystem();
        fileSystem.SeedFile(databasePath, "old-database-content");

        var service = new BackupService(
            databasePath,
            fileSystem,
            new FakeBackupClock(DateTime.Now),
            new FakeBackupInteraction
            {
                PickedFile = FakeBackupInteraction.CreateImportFile("backup.db", "imported-content")
            });

        var result = await service.ImportBackupAsync();

        Assert.True(result.IsSuccessful);
        Assert.Equal("Backup imported", result.Message);
        Assert.Equal("imported-content", fileSystem.ReadFile(databasePath));
        Assert.False(fileSystem.Exists(tempPath));
        Assert.Equal((tempPath, databasePath, true), Assert.Single(fileSystem.CopyOperations));
        Assert.Contains(tempPath, fileSystem.DeletedPaths);
    }

    [Fact]
    public async Task ImportBackupAsync_WhenTargetCopyFails_DeletesTempAndReturnsFailure()
    {
        var databasePath = "tasktracker.db";
        var tempPath = databasePath + ".tmp";
        var fileSystem = new FakeBackupFileSystem
        {
            ThrowOnCopy = true
        };
        fileSystem.SeedFile(databasePath, "old-database-content");

        var service = new BackupService(
            databasePath,
            fileSystem,
            new FakeBackupClock(DateTime.Now),
            new FakeBackupInteraction
            {
                PickedFile = FakeBackupInteraction.CreateImportFile("backup.db", "imported-content")
            });

        var result = await service.ImportBackupAsync();

        Assert.False(result.IsSuccessful);
        Assert.False(result.IsCanceled);
        Assert.StartsWith("Import failed:", result.Message, StringComparison.Ordinal);
        Assert.Equal("old-database-content", fileSystem.ReadFile(databasePath));
        Assert.False(fileSystem.Exists(tempPath));
        Assert.Contains(tempPath, fileSystem.DeletedPaths);
    }

    private sealed class FakeBackupClock : IBackupClock
    {
        public FakeBackupClock(DateTime now)
        {
            Now = now;
        }

        public DateTime Now { get; }
    }

    private sealed class FakeBackupInteraction : IBackupInteraction
    {
        public BackupSaveResult SaveResult { get; set; } = new(true);

        public BackupImportFile? PickedFile { get; set; }

        public string? SavedFileName { get; private set; }

        public string? SavedContent { get; private set; }

        public async Task<BackupSaveResult> SaveBackupAsync(string fileName, Stream stream, CancellationToken cancellationToken)
        {
            SavedFileName = fileName;
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            SavedContent = await reader.ReadToEndAsync(cancellationToken);
            return SaveResult;
        }

        public Task<BackupImportFile?> PickBackupToImportAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(PickedFile);
        }

        public static BackupImportFile CreateImportFile(string fileName, string content)
        {
            return new BackupImportFile(
                fileName,
                _ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(content))));
        }
    }

    private sealed class FakeBackupFileSystem : IBackupFileSystem
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

        public List<(string Source, string Destination, bool Overwrite)> CopyOperations { get; } = new();

        public List<string> DeletedPaths { get; } = new();

        public bool ThrowOnCopy { get; set; }

        public Stream OpenRead(string path)
        {
            if (!_files.TryGetValue(path, out var content))
            {
                throw new FileNotFoundException($"File not found: {path}", path);
            }

            return new MemoryStream(content, writable: false);
        }

        public Stream Create(string path)
        {
            return new CommitOnDisposeStream(bytes => _files[path] = bytes);
        }

        public void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            CopyOperations.Add((sourceFileName, destFileName, overwrite));

            if (ThrowOnCopy)
            {
                throw new IOException("Simulated copy failure.");
            }

            if (!_files.TryGetValue(sourceFileName, out var content))
            {
                throw new FileNotFoundException($"File not found: {sourceFileName}", sourceFileName);
            }

            if (!overwrite && _files.ContainsKey(destFileName))
            {
                throw new IOException("Destination file already exists.");
            }

            _files[destFileName] = content.ToArray();
        }

        public void Delete(string path)
        {
            DeletedPaths.Add(path);
            _files.Remove(path);
        }

        public bool Exists(string path)
        {
            return _files.ContainsKey(path);
        }

        public void SeedFile(string path, string content)
        {
            _files[path] = Encoding.UTF8.GetBytes(content);
        }

        public string ReadFile(string path)
        {
            return Encoding.UTF8.GetString(_files[path]);
        }
    }

    private sealed class CommitOnDisposeStream : MemoryStream
    {
        private readonly Action<byte[]> _commit;
        private bool _committed;

        public CommitOnDisposeStream(Action<byte[]> commit)
        {
            _commit = commit;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_committed)
            {
                _commit(ToArray());
                _committed = true;
            }

            base.Dispose(disposing);
        }
    }
}