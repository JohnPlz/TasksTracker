using LiteDB;
using TaskTracker.Models;

namespace TaskTracker.Services;

public class TaskRepository
{
    private const string CollectionName = "tasks";
    private readonly string _dbPath;

    public string DatabasePath => _dbPath;

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

    public IReadOnlyList<TaskEntry> GetAllOrderedByDate(int size = -1)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<TaskEntry>(CollectionName);
        if (size == -1)
        {
            return collection.FindAll()
            .OrderByDescending(item => item.StartTime)
            .ToList();
        }
        else
        {
            return collection.FindAll()
            .OrderByDescending(item => item.StartTime)
            .Take(size)
            .ToList();
        }
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
