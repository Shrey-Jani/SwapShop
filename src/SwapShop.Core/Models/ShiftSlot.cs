namespace SwapShop.Core.Models;

public record ShiftSlot(int? Id, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime)
{
    public ShiftSpan Span => ShiftSpan.From(Date, StartTime, EndTime);
}
