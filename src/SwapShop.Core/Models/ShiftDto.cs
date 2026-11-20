using SwapShop.Core.Enums;

namespace SwapShop.Core.Models;

public record ShiftDto(
    int Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Position Position,
    string? AssignedUserId,
    string? AssignedUserName,
    string? Notes,
    bool IsPublished
)
{
    public bool IsOpen => AssignedUserId == null; 

    public TimeSpan Duration => EndTime - StartTime;
}