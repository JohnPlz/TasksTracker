using LiteDB;
using TaskTracker.Models;

namespace TaskTracker.Services;

public class TaskRepository
{
    private const string CollectionName = "tasks";
    private readonly string _dbPath;

    public TaskRepository(string? dbPath = null)
    {
        _dbPath = dbPath ?? Path.Combine(FileSystem.AppDataDirectory, "tasktracker.db");

        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<TaskEntry>(CollectionName);
        collection.EnsureIndex(item => item.StartTime);
    }

    public IReadOnlyList<TaskEntry> GetAll()
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<TaskEntry>(CollectionName);
        return collection.FindAll()
            .OrderByDescending(item => item.StartTime)
            .ToList();
    }

    public TaskEntry Add(TaskEntry entry)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<TaskEntry>(CollectionName);
        var id = collection.Insert(entry);
        if (entry.Id == 0)
        {
            entry.Id = id.AsInt32;
        }
        return entry;
    }

    public void Delete(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<TaskEntry>(CollectionName);
        collection.Delete(id);
    }
}
