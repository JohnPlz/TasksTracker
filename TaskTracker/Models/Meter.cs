using System;
using TaskTracker.Models.Enums;

namespace TaskTracker.Models;

public class Meter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MeterCategory Category { get; set; }
    public string Number { get; set; } = string.Empty;
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public string Note { get; set; } = string.Empty;
    public bool IsDeactivated { get; set; }
}
