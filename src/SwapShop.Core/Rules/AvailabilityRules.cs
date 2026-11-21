using SwapShop.Core.Models;

namespace SwapShop.Core.Rules;

public static class AvailabilityRules
{
    public static IReadOnlyList<string> Validate(IEnumerable<AvailabilityWindow> windows)
    {
        var errors = new List<string>();
        var all = windows.ToList();

        foreach (var window in all.Where(w => w.End <= w.Start))
        {
            errors.Add($"{window.Day}: {window.Start:HH:mm}–{window.End:HH:mm} is invalid, the end must be after the start.");
        }

        var validByDay = all
            .Where(w => w.End > w.Start)
            .GroupBy(w => w.Day);

        foreach (var day in validByDay)
        {
            var ordered = day.OrderBy(w => w.Start).ToList();
            var latestEnd = ordered[0].End;

            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].Start < latestEnd)
                {
                    errors.Add($"{day.Key}: the window starting at {ordered[i].Start:HH:mm} overlaps an earlier window.");
                }

                if (ordered[i].End > latestEnd)
                {
                    latestEnd = ordered[i].End;
                }
            }
        }

        return errors;
    }
}
