using SwapShop.Core.Enums;
using SwapShop.Core.Models;
using SwapShop.Core.Rules;

namespace SwapShop.Tests;

public class ConflictCheckerTimeOffTests
{
    // Monday 23 Nov 2026 is the base day, so the 24th is a Tuesday.
    private static DateOnly Day(int day) => new(2026, 11, day);

    private static ShiftSlot Slot(int day, int startHour, int endHour, int? id = 1) =>
        new(id, Day(day), new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    private static DateRange Leave(int startDay, int endDay) => new(Day(startDay), Day(endDay));

    [Fact]
    public void NoTimeOffDataSupplied_ProducesNoConflict()
    {
        var conflicts = ConflictChecker.Check(Slot(24, 9, 17), [], null, null);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void EmptyTimeOffList_ProducesNoConflict()
    {
        var conflicts = ConflictChecker.Check(Slot(24, 9, 17), [], approvedTimeOff: []);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void ShiftOnTheLeaveDay_IsOneTimeOffConflict()
    {
        var conflict = Assert.Single(ConflictChecker.Check(Slot(24, 9, 17), [], approvedTimeOff: [Leave(24, 24)]));

        Assert.Equal(ConflictType.TimeOff, conflict.Type);
        Assert.Null(conflict.RelatedShiftId);
    }

    [Fact]
    public void ShiftTheDayBeforeOrAfterTheLeave_ProducesNoConflict()
    {
        Assert.Empty(ConflictChecker.Check(Slot(23, 9, 17), [], approvedTimeOff: [Leave(24, 24)]));
        Assert.Empty(ConflictChecker.Check(Slot(25, 9, 17), [], approvedTimeOff: [Leave(24, 24)]));
    }

    [Fact]
    public void ShiftInsideAnOlderMultiDayLeave_IsOneConflict()
    {
        Assert.Single(ConflictChecker.Check(Slot(26, 9, 17), [], approvedTimeOff: [Leave(24, 28)]));
    }

    [Fact]
    public void FirstAndLastDayOfAMultiDayLeave_AreBothConflicts()
    {
        Assert.Single(ConflictChecker.Check(Slot(24, 9, 17), [], approvedTimeOff: [Leave(24, 28)]));
        Assert.Single(ConflictChecker.Check(Slot(28, 9, 17), [], approvedTimeOff: [Leave(24, 28)]));
    }

    [Fact]
    public void ShiftTheDayAfterAMultiDayLeave_ProducesNoConflict()
    {
        Assert.Empty(ConflictChecker.Check(Slot(29, 9, 17), [], approvedTimeOff: [Leave(24, 28)]));
    }

    [Fact]
    public void OvernightShift_WhoseTailFallsOnTheLeaveDay_IsAConflict()
    {
        // Monday 22:00 to Tuesday 02:00, leave on Tuesday.
        Assert.Single(ConflictChecker.Check(Slot(23, 22, 2), [], approvedTimeOff: [Leave(24, 24)]));
    }

    [Fact]
    public void OvernightShift_StartingOnTheLeaveDay_IsAConflict()
    {
        // Monday 22:00 to Tuesday 02:00, leave on Monday.
        Assert.Single(ConflictChecker.Check(Slot(23, 22, 2), [], approvedTimeOff: [Leave(23, 23)]));
    }

    [Fact]
    public void ShiftEndingExactlyAtMidnightBeforeTheLeave_ProducesNoConflict()
    {
        // Monday 18:00 to midnight, leave on Tuesday.
        Assert.Empty(ConflictChecker.Check(Slot(23, 18, 0), [], approvedTimeOff: [Leave(24, 24)]));
    }

    [Fact]
    public void ShiftStartingExactlyAtMidnightAfterTheLeave_ProducesNoConflict()
    {
        // Wednesday 00:00 to 06:00, leave on Tuesday.
        Assert.Empty(ConflictChecker.Check(Slot(25, 0, 6), [], approvedTimeOff: [Leave(24, 24)]));
    }

    [Fact]
    public void OneOvernightShiftTouchingTwoLeaveRanges_GivesTwoConflicts()
    {
        var conflicts = ConflictChecker.Check(
            Slot(23, 22, 2), [], approvedTimeOff: [Leave(23, 23), Leave(24, 24)]);

        Assert.Equal(2, conflicts.Count);
    }

    [Fact]
    public void Message_ForASingleDayLeave_NamesTheDate()
    {
        var message = Assert.Single(
            ConflictChecker.Check(Slot(24, 9, 17), [], approvedTimeOff: [Leave(24, 24)])).Message;

        Assert.Contains("Tue, Nov 24", message);
    }

    [Fact]
    public void Message_ForAMultiDayLeave_NamesBothDates()
    {
        var message = Assert.Single(
            ConflictChecker.Check(Slot(26, 9, 17), [], approvedTimeOff: [Leave(24, 28)])).Message;

        Assert.Contains("Nov 24", message);
        Assert.Contains("Nov 28", message);
    }

    [Fact]
    public void AllThreeRules_AreReportedTogetherInOrder()
    {
        var availability = new Dictionary<DateOnly, WeekAvailability>
        {
            [Day(23)] = new(
                AvailabilitySource.Standing,
                [new AvailabilityWindow(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(16, 0))])
        };

        var conflicts = ConflictChecker.Check(
            Slot(23, 9, 17),
            [Slot(23, 16, 20, id: 2)],
            availability,
            [Leave(23, 23)]);

        Assert.Equal(
            [ConflictType.Overlap, ConflictType.OutsideAvailability, ConflictType.TimeOff],
            conflicts.Select(c => c.Type));
    }
}
