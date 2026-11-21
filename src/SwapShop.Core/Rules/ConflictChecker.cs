using SwapShop.Core.Enums;
using SwapShop.Core.Models;

namespace SwapShop.Core.Rules;

public static class ConflictChecker
{
    public static IReadOnlyList<Conflict> Check(ShiftSlot shift, IEnumerable<ShiftSlot> otherShifts)
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

        return conflicts;
    }
}
