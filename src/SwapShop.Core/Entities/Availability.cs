namespace SwapShop.Core.Entities;

public class Availability
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
