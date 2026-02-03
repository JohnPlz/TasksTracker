using LiteDB;
using TaskTracker.Models;

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

    public IReadOnlyList<Meter> GetAll()
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Meter>(CollectionName);
        return collection.FindAll()
            .OrderBy(item => item.Name)
            .ToList();
    }

    public Meter? GetById(int id)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Meter>(CollectionName);
        return collection.FindById(id);
    }

    public Meter Upsert(Meter meter)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Meter>(CollectionName);
        if (meter.Id == 0)
        {
            var id = collection.Insert(meter);
            meter.Id = id.AsInt32;
        }
        else
        {
            collection.Update(meter);
        }
        return meter;
    }

    public Position AddPosition(int meterId, double value, DateTime? addedAt = null)
    {
        using var db = new LiteDatabase(_dbPath);
        var collection = db.GetCollection<Meter>(CollectionName);
        var meter = collection.FindById(meterId);
        if (meter is null)
        {
            throw new InvalidOperationException("Meter not found.");
        }

        var nextId = meter.Positions.Count == 0 ? 1 : meter.Positions.Max(position => position.Id) + 1;
        var position = new Position
        {
            Id = nextId,
            Value = value,
            AddedAt = addedAt ?? DateTime.Now
        };

        meter.Positions.Add(position);
        collection.Update(meter);
        return position;
    }
}
