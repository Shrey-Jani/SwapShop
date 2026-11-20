using SwapShop.Core.Enums;

namespace SwapShop.Core.Entities;

public class Shift
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public Position Position { get; set; }
    public string? AssignedUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsPublished { get; set; }
}