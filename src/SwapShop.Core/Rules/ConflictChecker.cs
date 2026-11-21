using SwapShop.Core.Enums;
using SwapShop.Core.Models;

namespace SwapShop.Core.Rules;

public static class ConflictChecker
{
    public static IReadOnlyList<Conflict> Check(
        ShiftSlot shift,
        IEnumerable<ShiftSlot> otherShifts,
        IReadOnlyDictionary<DateOnly, WeekAvailability>? availabilityByWeek = null,
        IReadOnlyList<DateRange>? approvedTimeOff = null)
    {
        var span = shift.Span;
        var conflicts = new List<Conflict>();

        foreach (var other in otherShifts)
        {
            if (shift.Id is not null && other.Id == shift.Id)
            {
                continue;
            }

            if (span.Overlaps(other.Span))
            {
                conflicts.Add(new Conflict(
                    ConflictType.Overlap,
                    $"Overlaps another shift on {other.Date:ddd, MMM d} ({other.StartTime:HH:mm}–{other.EndTime:HH:mm}).",
                    other.Id));
            }
        }

        if (availabilityByWeek is not null)
        {
            conflicts.AddRange(AvailabilityCheck.Check(shift, availabilityByWeek));
        }

        if (approvedTimeOff is not null)
        {
            foreach (var leave in approvedTimeOff.Where(l => span.Overlaps(l)))
            {
                conflicts.Add(new Conflict(ConflictType.TimeOff, LeaveMessage(leave)));
            }
        }

        return conflicts;
    }

    private static string LeaveMessage(DateRange leave) =>
        leave.Start == leave.End
            ? $"Approved time off on {leave.Start:ddd, MMM d}."
            : $"Approved time off from {leave.Start:MMM d} to {leave.End:MMM d}.";
}
