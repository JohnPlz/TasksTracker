using System.Diagnostics.Metrics;
using LiteDB;

namespace TaskTracker.Services;

public class MeterRepository
{
    private const string CollectionName = "meters";
    private readonly string _dbPath;

    public string DatabasePath => _dbPath;

    public MeterRepository(string? dbPath = null)
    {
        _dbPath = dbPath ?? Path.Combine(FileSystem.AppDataDirectory, "tasktracker.db");

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Meter>(CollectionName);
        collection.EnsureIndex(item => item.Name);
    }
}
