using SwapShop.Core.Models;
using SwapShop.Core.Rules;

namespace SwapShop.Tests;

public class AffectedShiftsTests
{
    private static DateOnly Day(int day) => new(2026, 11, day);

    private static ShiftSpan Shift(int day, int startHour, int endHour) =>
        ShiftSpan.From(Day(day), new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    [Fact]
    public void ShiftInsideTheLeave_IsCounted()
    {
        var count = TimeOffRules.CountAffectedShifts([Shift(25, 9, 17)], Day(24), Day(26));

        Assert.Equal(1, count);
    }

    [Fact]
    public void ShiftOnTheLastDayOfTheLeave_IsCounted()
    {
        var count = TimeOffRules.CountAffectedShifts([Shift(26, 9, 17)], Day(24), Day(26));

        Assert.Equal(1, count);
    }

    [Fact]
    public void ShiftTheDayAfterTheLeave_IsNotCounted()
    {
        var count = TimeOffRules.CountAffectedShifts([Shift(27, 9, 17)], Day(24), Day(26));

        Assert.Equal(0, count);
    }

    [Fact]
    public void OvernightShiftSpillingIntoTheLeave_IsCounted()
    {
        var overnight = ShiftSpan.From(Day(23), new TimeOnly(22, 0), new TimeOnly(2, 0));

        var count = TimeOffRules.CountAffectedShifts([overnight], Day(24), Day(24));

        Assert.Equal(1, count);
    }

    [Fact]
    public void OvernightShiftThatEndsAtMidnightBeforeTheLeave_IsNotCounted()
    {
        var shift = ShiftSpan.From(Day(23), new TimeOnly(18, 0), new TimeOnly(0, 0));

        var count = TimeOffRules.CountAffectedShifts([shift], Day(24), Day(24));

        Assert.Equal(0, count);
    }

    [Fact]
    public void SeveralShifts_AreAllCounted()
    {
        var count = TimeOffRules.CountAffectedShifts(
            [Shift(24, 9, 17), Shift(25, 9, 17), Shift(30, 9, 17)],
            Day(24),
            Day(26));

        Assert.Equal(2, count);
    }

    [Fact]
    public void NoShifts_IsZero()
    {
        var count = TimeOffRules.CountAffectedShifts([], Day(24), Day(26));

        Assert.Equal(0, count);
    }
}
