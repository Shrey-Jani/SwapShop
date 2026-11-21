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

    public bool Overlaps(ShiftSpan other) => Start < other.End && End > other.Start;

    public bool Overlaps(DateRange leave)
    {
        var leaveFrom = leave.Start.ToDateTime(TimeOnly.MinValue);
        var leaveTo = leave.End.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return Start < leaveTo && End > leaveFrom;
    }
}
