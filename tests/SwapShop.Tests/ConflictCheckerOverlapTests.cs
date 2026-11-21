using SwapShop.Core.Enums;
using SwapShop.Core.Models;
using SwapShop.Core.Rules;

namespace SwapShop.Tests;

public class ConflictCheckerOverlapTests
{
    // Monday 23 Nov 2026 is the base day for these tests.
    private static DateOnly Day(int day) => new(2026, 11, day);

    private static ShiftSlot Slot(int? id, int day, int startHour, int endHour) =>
        new(id, Day(day), new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    [Fact]
    public void NoOtherShifts_HasNoConflicts()
    {
        var conflicts = ConflictChecker.Check(Slot(1, 23, 9, 17), []);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void ShiftOnADifferentDay_HasNoConflicts()
    {
        var conflicts = ConflictChecker.Check(Slot(1, 23, 9, 17), [Slot(2, 24, 9, 17)]);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void ShiftsWithAGapBetweenThem_HaveNoConflicts()
    {
        var conflicts = ConflictChecker.Check(Slot(1, 23, 9, 13), [Slot(2, 23, 14, 18)]);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void BackToBackShifts_HaveNoConflicts()
    {
        var conflicts = ConflictChecker.Check(Slot(1, 23, 9, 17), [Slot(2, 23, 17, 22)]);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void PartialOverlap_IsOneOverlapConflictPointingAtTheOtherShift()
    {
        var conflicts = ConflictChecker.Check(Slot(1, 23, 9, 17), [Slot(2, 23, 16, 20)]);

        var conflict = Assert.Single(conflicts);
        Assert.Equal(ConflictType.Overlap, conflict.Type);
        Assert.Equal(2, conflict.RelatedShiftId);
    }

    [Fact]
    public void IdenticalTimes_Conflict()
    {
        var conflicts = ConflictChecker.Check(Slot(1, 23, 9, 17), [Slot(2, 23, 9, 17)]);

        Assert.Single(conflicts);
    }

    [Fact]
    public void OtherShiftInsideThisOne_Conflicts()
    {
        var conflicts = ConflictChecker.Check(Slot(1, 23, 9, 17), [Slot(2, 23, 10, 12)]);

        Assert.Single(conflicts);
    }

    [Fact]
    public void ThisShiftInsideTheOtherOne_Conflicts()
    {
        var conflicts = ConflictChecker.Check(Slot(1, 23, 10, 12), [Slot(2, 23, 9, 17)]);

        Assert.Single(conflicts);
    }

    [Fact]
    public void OvernightShift_ConflictsWithAnEarlyShiftTheNextMorning()
    {
        // Monday 22:00 to Tuesday 02:00 against Tuesday 01:00 to 05:00.
        var conflicts = ConflictChecker.Check(Slot(1, 23, 22, 2), [Slot(2, 24, 1, 5)]);

        Assert.Single(conflicts);
    }

    [Fact]
    public void ShiftEarlyInTheMorning_ConflictsWithLastNightsOvernightShift()
    {
        // Same two shifts, checked from the other side.
        var conflicts = ConflictChecker.Check(Slot(2, 24, 1, 5), [Slot(1, 23, 22, 2)]);

        Assert.Single(conflicts);
    }

    [Fact]
    public void ShiftStartingExactlyWhenAnOvernightShiftEnds_HasNoConflict()
    {
        // Monday 22:00 to Tuesday 02:00, then Tuesday 02:00 to 06:00.
        var conflicts = ConflictChecker.Check(Slot(1, 23, 22, 2), [Slot(2, 24, 2, 6)]);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void EditingAShift_DoesNotConflictWithItself()
    {
        var conflicts = ConflictChecker.Check(Slot(7, 23, 9, 17), [Slot(7, 23, 9, 17)]);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void NewShiftWithNoId_IsCheckedAgainstEveryOtherShift()
    {
        var conflicts = ConflictChecker.Check(Slot(null, 23, 9, 17), [Slot(2, 23, 9, 17)]);

        Assert.Single(conflicts);
    }

    [Fact]
    public void TwoOverlappingShifts_GiveTwoConflicts()
    {
        var conflicts = ConflictChecker.Check(
            Slot(1, 23, 9, 17),
            [Slot(2, 23, 8, 10), Slot(3, 23, 16, 20), Slot(4, 23, 18, 22)]);

        Assert.Equal(2, conflicts.Count);
        Assert.Equal([2, 3], conflicts.Select(c => c.RelatedShiftId));
    }

    [Fact]
    public void Message_NamesTheOtherShiftsDateAndTimes()
    {
        var conflicts = ConflictChecker.Check(Slot(1, 23, 9, 17), [Slot(2, 23, 16, 20)]);

        var message = Assert.Single(conflicts).Message;
        Assert.Contains("Nov 23", message);
        Assert.Contains("16:00", message);
        Assert.Contains("20:00", message);
    }
}
