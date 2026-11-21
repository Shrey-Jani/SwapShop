namespace SwapShop.Core.Entities;

public class Availability
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly? WeekStart { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
