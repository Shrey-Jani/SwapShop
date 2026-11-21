using SwapShop.Core.Models;

namespace SwapShop.Core.Rules;

public static class TimeOffRules
{
    public static IReadOnlyList<string> Validate(
        DateOnly date,
        DateOnly today,
        IEnumerable<DateRange> existingActiveRequests)
    {
        var errors = new List<string>();

        if (date < today)
        {
            errors.Add("Time off cannot be requested for a day in the past.");
        }

        var requested = new DateRange(date, date);
        if (existingActiveRequests.Any(r => r.Overlaps(requested)))
        {
            errors.Add("You already have a pending or approved time-off request for this day.");
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

    private static bool OverlapsLeave(ShiftSpan shift, DateOnly leaveStart, DateOnly leaveEnd) =>
        shift.Overlaps(new DateRange(leaveStart, leaveEnd));
}
