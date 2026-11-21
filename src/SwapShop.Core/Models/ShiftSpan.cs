namespace SwapShop.Core.Models;

public record ShiftSpan(DateTime Start, DateTime End)
{
    public static ShiftSpan From(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        var start = date.ToDateTime(startTime);
        var end = date.ToDateTime(endTime);

        if (endTime <= startTime)
        {
            end = end.AddDays(1);
        }

        return new ShiftSpan(start, end);
    }
}
