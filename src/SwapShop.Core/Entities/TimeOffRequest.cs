using SwapShop.Core.Enums;

namespace SwapShop.Core.Entities;

public class TimeOffRequest
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Reason { get; set; }
    public TimeOffStatus Status { get; set; } = TimeOffStatus.Pending;
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
