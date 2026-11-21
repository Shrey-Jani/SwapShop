using SwapShop.Core.Models;

namespace SwapShop.Web.Components.Scheduling;

public class WindowEdit
{
    public DayOfWeek Day { get; init; }
    public TimeSpan? Start { get; set; }
    public TimeSpan? End { get; set; }

    public static List<WindowEdit> FromWindows(IEnumerable<AvailabilityWindow> windows) =>
        windows
            .Select(w => new WindowEdit { Day = w.Day, Start = w.Start.ToTimeSpan(), End = w.End.ToTimeSpan() })
            .ToList();

    public static bool TryToWindows(IEnumerable<WindowEdit> edits, out List<AvailabilityWindow> windows)
    {
        windows = [];

        foreach (var edit in edits)
        {
            if (edit.Start is null || edit.End is null)
            {
                return false;
            }

            windows.Add(new AvailabilityWindow(
                edit.Day,
                TimeOnly.FromTimeSpan(edit.Start.Value),
                TimeOnly.FromTimeSpan(edit.End.Value)));
        }

        return true;
    }
}
