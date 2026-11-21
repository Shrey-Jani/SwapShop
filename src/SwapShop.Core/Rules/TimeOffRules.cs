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
        var leaveFrom = leaveStart.ToDateTime(TimeOnly.MinValue);
        var leaveTo = leaveEnd.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var horizon = now.AddHours(24);

        return shifts.Any(s =>
            s.Start < leaveTo && s.End > leaveFrom
            && s.Start <= horizon && s.End > now);
    }
}
