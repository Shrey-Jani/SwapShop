using SwapShop.Core.Enums;
using SwapShop.Core.Models;
using SwapShop.Core.Utilities;

namespace SwapShop.Core.Rules;

internal static class AvailabilityCheck
{
    private const int MinutesPerDay = 24 * 60;
    private const int EndOfDayFrom = MinutesPerDay - 1;

    public static IReadOnlyList<Conflict> Check(
        ShiftSlot shift,
        IReadOnlyDictionary<DateOnly, WeekAvailability> availabilityByWeek)
    {
        var span = shift.Span;
        var conflicts = new List<Conflict>();

        for (var day = DateOnly.FromDateTime(span.Start);
             day.ToDateTime(TimeOnly.MinValue) < span.End;
             day = day.AddDays(1))
        {
            var dayStart = day.ToDateTime(TimeOnly.MinValue);
            var nextDayStart = dayStart.AddDays(1);

            var pieceStart = span.Start > dayStart ? span.Start : dayStart;
            var pieceEnd = span.End < nextDayStart ? span.End : nextDayStart;

            var from = (int)(pieceStart - dayStart).TotalMinutes;
            var to = (int)(pieceEnd - dayStart).TotalMinutes;

            if (!availabilityByWeek.TryGetValue(WeekHelper.StartOfWeek(day), out var week)
                || week.Source == AvailabilitySource.None)
            {
                continue;
            }

            var windows = MergedWindows(week.Windows, day.DayOfWeek);

            if (windows.Count == 0)
            {
                conflicts.Add(new Conflict(
                    ConflictType.OutsideAvailability,
                    $"{day:dddd, MMM d}: not available that day."));
            }
            else if (!windows.Any(w => w.Start <= from && w.End >= to))
            {
                var hours = string.Join(", ", windows.Select(w => $"{Format(w.Start)}–{Format(w.End)}"));

                conflicts.Add(new Conflict(
                    ConflictType.OutsideAvailability,
                    $"{day:dddd, MMM d}: outside available hours ({hours})."));
            }
        }

        return conflicts;
    }

    private static List<(int Start, int End)> MergedWindows(
        IEnumerable<AvailabilityWindow> windows, DayOfWeek day)
    {
        var ranges = windows
            .Where(w => w.Day == day)
            .Select(w => (Start: Minutes(w.Start), End: EndMinutes(w.End)))
            .OrderBy(r => r.Start)
            .ToList();

        var merged = new List<(int Start, int End)>();

        foreach (var range in ranges)
        {
            if (merged.Count > 0 && range.Start <= merged[^1].End)
            {
                merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, range.End));
            }
            else
            {
                merged.Add(range);
            }
        }

        return merged;
    }

    private static int Minutes(TimeOnly time) => time.Hour * 60 + time.Minute;

    private static int EndMinutes(TimeOnly time) =>
        Minutes(time) >= EndOfDayFrom ? MinutesPerDay : Minutes(time);

    private static string Format(int minutes) =>
        minutes == MinutesPerDay ? "24:00" : $"{minutes / 60:00}:{minutes % 60:00}";
}
