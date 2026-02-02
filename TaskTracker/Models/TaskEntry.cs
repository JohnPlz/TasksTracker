using LiteDB;

namespace TaskTracker.Models;

public class TaskEntry
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationMinutes { get; set; }

    [BsonIgnore]
    public TimeSpan Duration => TimeSpan.FromMinutes(DurationMinutes);

    [BsonIgnore]
    public string DurationText => Duration.ToString(@"hh\:mm");
}
