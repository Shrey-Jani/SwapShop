using SwapShop.Core.Enums;

namespace SwapShop.Core.Models;

public record TimeOffRequestDto(
    int Id,
    string UserId,
    string EmployeeName,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason,
    TimeOffStatus Status,
    bool IsShortNotice,
    DateTime RequestedAtUtc,
    DateTime? ReviewedAtUtc)
{
    public int Days => EndDate.DayNumber - StartDate.DayNumber + 1;
}
