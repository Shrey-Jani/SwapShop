using SwapShop.Core.Models;

namespace SwapShop.Core.Rules;

public static class TimeOffRules
{
    public static IReadOnlyList<string> Validate(
        DateOnly start,
        DateOnly end,
        DateOnly today,
        IEnumerable<DateRange> existingActiveRequests)
    {
        var errors = new List<string>();

        if (end < start)
        {
            errors.Add("The last day off cannot be before the first day off.");
            return errors;
        }

        if (start < today)
        {
            errors.Add("Time off cannot start in the past.");
        }

        var requested = new DateRange(start, end);
        if (existingActiveRequests.Any(r => r.Overlaps(requested)))
        {
            errors.Add("This overlaps another pending or approved time-off request.");
        }

        return errors;
    }

    public static bool IsShortNotice(
        IEnumerable<ShiftSpan> shifts,
        DateOnly leaveStart,
        DateOnly leaveEnd,
        DateTime now)
    {
        var horizon = now.AddHours(24);

        return shifts.Any(s =>
            OverlapsLeave(s, leaveStart, leaveEnd)
            && s.Start <= horizon && s.End > now);
    }

    public static int CountAffectedShifts(
        IEnumerable<ShiftSpan> shifts,
        DateOnly leaveStart,
        DateOnly leaveEnd) =>
        shifts.Count(s => OverlapsLeave(s, leaveStart, leaveEnd));

    private static bool OverlapsLeave(ShiftSpan shift, DateOnly leaveStart, DateOnly leaveEnd)
    {
        var leaveFrom = leaveStart.ToDateTime(TimeOnly.MinValue);
        var leaveTo = leaveEnd.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return shift.Start < leaveTo && shift.End > leaveFrom;
    }
}
