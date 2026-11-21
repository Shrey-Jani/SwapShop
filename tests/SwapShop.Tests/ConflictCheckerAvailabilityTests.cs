using SwapShop.Core.Enums;
using SwapShop.Core.Models;
using SwapShop.Core.Rules;

namespace SwapShop.Tests;

public class ConflictCheckerAvailabilityTests
{
    // Week A starts on Monday 23 Nov 2026, week B on Monday 30 Nov 2026.
    private static readonly DateOnly WeekA = new(2026, 11, 23);
    private static readonly DateOnly WeekB = new(2026, 11, 30);

    private static DateOnly Day(int day) => new(2026, 11, day);

    private static AvailabilityWindow Window(DayOfWeek day, int startHour, int endHour, int endMinute = 0) =>
        new(day, new TimeOnly(startHour, 0), new TimeOnly(endHour, endMinute));

    private static ShiftSlot Slot(int day, int startHour, int endHour, int? id = 1) =>
        new(id, Day(day), new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    private static IReadOnlyDictionary<DateOnly, WeekAvailability> WeekAOnly(
        AvailabilitySource source, params AvailabilityWindow[] windows) =>
        new Dictionary<DateOnly, WeekAvailability> { [WeekA] = new(source, windows) };

    private static IReadOnlyDictionary<DateOnly, WeekAvailability> BothWeeks(
        AvailabilityWindow[] weekA, AvailabilityWindow[] weekB) =>
        new Dictionary<DateOnly, WeekAvailability>
        {
            [WeekA] = new(AvailabilitySource.Standing, weekA),
            [WeekB] = new(AvailabilitySource.Custom, weekB)
        };

    [Fact]
    public void NoAvailabilityDataSupplied_ProducesNoConflict()
    {
        var conflicts = ConflictChecker.Check(Slot(23, 9, 17), [], null);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void NothingSetForTheWeek_ProducesNoConflict()
    {
        var conflicts = ConflictChecker.Check(Slot(23, 9, 17), [], WeekAOnly(AvailabilitySource.None));

        Assert.Empty(conflicts);
    }

    [Fact]
    public void ShiftInsideAWindow_ProducesNoConflict()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Monday, 8, 18));

        Assert.Empty(ConflictChecker.Check(Slot(23, 9, 17), [], availability));
    }

    [Fact]
    public void ShiftExactlyFillingAWindow_ProducesNoConflict()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Monday, 9, 17));

        Assert.Empty(ConflictChecker.Check(Slot(23, 9, 17), [], availability));
    }

    [Fact]
    public void ShiftStartingBeforeTheWindow_IsOneAvailabilityConflict()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Monday, 10, 18));

        var conflict = Assert.Single(ConflictChecker.Check(Slot(23, 9, 17), [], availability));

        Assert.Equal(ConflictType.OutsideAvailability, conflict.Type);
        Assert.Null(conflict.RelatedShiftId);
    }

    [Fact]
    public void ShiftEndingAfterTheWindow_IsOneAvailabilityConflict()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Monday, 8, 16));

        var conflict = Assert.Single(ConflictChecker.Check(Slot(23, 9, 17), [], availability));

        Assert.Equal(ConflictType.OutsideAvailability, conflict.Type);
    }

    [Fact]
    public void DayWithNoWindows_MeansNotAvailableThatDay()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Monday, 9, 17));

        var conflict = Assert.Single(ConflictChecker.Check(Slot(24, 9, 17), [], availability));

        Assert.Equal(ConflictType.OutsideAvailability, conflict.Type);
        Assert.Contains("not available", conflict.Message);
    }

    [Fact]
    public void TouchingWindows_TogetherCoverTheShift()
    {
        var availability = WeekAOnly(
            AvailabilitySource.Standing,
            Window(DayOfWeek.Monday, 9, 12),
            Window(DayOfWeek.Monday, 12, 17));

        Assert.Empty(ConflictChecker.Check(Slot(23, 11, 14), [], availability));
    }

    [Fact]
    public void WindowsGivenOutOfOrder_StillCoverTheShift()
    {
        var availability = WeekAOnly(
            AvailabilitySource.Standing,
            Window(DayOfWeek.Monday, 12, 17),
            Window(DayOfWeek.Monday, 9, 12));

        Assert.Empty(ConflictChecker.Check(Slot(23, 11, 14), [], availability));
    }

    [Fact]
    public void GapBetweenWindows_IsAConflict()
    {
        var availability = WeekAOnly(
            AvailabilitySource.Standing,
            Window(DayOfWeek.Monday, 9, 12),
            Window(DayOfWeek.Monday, 13, 17));

        Assert.Single(ConflictChecker.Check(Slot(23, 11, 14), [], availability));
    }

    [Fact]
    public void WindowEndingAt2359_CountsAsTheEndOfTheDay()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Monday, 12, 23, 59));

        // Monday 18:00 to midnight.
        Assert.Empty(ConflictChecker.Check(Slot(23, 18, 0), [], availability));
    }

    [Fact]
    public void ShiftEndingAtMidnight_IsOutsideAWindowEndingAt22()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Monday, 12, 22));

        Assert.Single(ConflictChecker.Check(Slot(23, 18, 0), [], availability));
    }

    [Fact]
    public void CustomAvailability_IsCheckedLikeStanding()
    {
        var availability = WeekAOnly(AvailabilitySource.Custom, Window(DayOfWeek.Monday, 12, 16));

        Assert.Single(ConflictChecker.Check(Slot(23, 9, 17), [], availability));
    }

    [Fact]
    public void OvernightShift_WithBothDaysCovered_ProducesNoConflict()
    {
        var availability = WeekAOnly(
            AvailabilitySource.Standing,
            Window(DayOfWeek.Monday, 18, 23, 59),
            Window(DayOfWeek.Tuesday, 0, 6));

        // Monday 22:00 to Tuesday 02:00.
        Assert.Empty(ConflictChecker.Check(Slot(23, 22, 2), [], availability));
    }

    [Fact]
    public void OvernightShift_WithTheNextDayNotCovered_IsOneConflict()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Monday, 18, 23, 59));

        var conflict = Assert.Single(ConflictChecker.Check(Slot(23, 22, 2), [], availability));

        Assert.Contains("Tuesday", conflict.Message);
    }

    [Fact]
    public void OvernightShift_FailingOnBothDays_IsTwoConflicts()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Wednesday, 9, 17));

        var conflicts = ConflictChecker.Check(Slot(23, 22, 2), [], availability);

        Assert.Equal(2, conflicts.Count);
    }

    [Fact]
    public void ShiftCrossingIntoNextWeek_UsesNextWeeksAvailabilityForTheLaterPart()
    {
        var availability = BothWeeks(
            weekA: [Window(DayOfWeek.Sunday, 18, 23, 59)],
            weekB: [Window(DayOfWeek.Monday, 0, 6)]);

        // Sunday 29 Nov 22:00 to Monday 30 Nov 02:00.
        Assert.Empty(ConflictChecker.Check(Slot(29, 22, 2), [], availability));
    }

    [Fact]
    public void ShiftCrossingIntoNextWeek_IsAConflictWhenNextWeekDoesNotCoverIt()
    {
        var availability = BothWeeks(
            weekA: [Window(DayOfWeek.Sunday, 18, 23, 59)],
            weekB: [Window(DayOfWeek.Monday, 8, 16)]);

        var conflict = Assert.Single(ConflictChecker.Check(Slot(29, 22, 2), [], availability));

        Assert.Contains("Monday", conflict.Message);
    }

    [Fact]
    public void WeekThatWasNotSupplied_IsTreatedAsNothingSet()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Sunday, 18, 23, 59));

        // The Monday part falls in week B, which is missing, so it produces no warning.
        Assert.Empty(ConflictChecker.Check(Slot(29, 22, 2), [], availability));
    }

    [Fact]
    public void Message_NamesTheDayAndTheAvailableHours()
    {
        var availability = WeekAOnly(
            AvailabilitySource.Standing,
            Window(DayOfWeek.Monday, 10, 12),
            Window(DayOfWeek.Monday, 13, 17));

        var message = Assert.Single(ConflictChecker.Check(Slot(23, 9, 17), [], availability)).Message;

        Assert.Contains("Monday, Nov 23", message);
        Assert.Contains("10:00–12:00", message);
        Assert.Contains("13:00–17:00", message);
    }

    [Fact]
    public void OverlapAndAvailabilityProblems_AreReportedTogether()
    {
        var availability = WeekAOnly(AvailabilitySource.Standing, Window(DayOfWeek.Monday, 12, 16));

        var conflicts = ConflictChecker.Check(Slot(23, 9, 17), [Slot(23, 16, 20, id: 2)], availability);

        Assert.Equal(2, conflicts.Count);
        Assert.Contains(conflicts, c => c.Type == ConflictType.Overlap);
        Assert.Contains(conflicts, c => c.Type == ConflictType.OutsideAvailability);
    }
}
