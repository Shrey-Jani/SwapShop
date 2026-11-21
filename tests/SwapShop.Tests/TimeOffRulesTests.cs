using SwapShop.Core.Models;
using SwapShop.Core.Rules;

namespace SwapShop.Tests;

public class TimeOffRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);

    private static DateOnly Day(int day) => new(2026, 11, day);

    private static DateRange Range(int startDay, int endDay) => new(Day(startDay), Day(endDay));

    [Fact]
    public void FutureDayWithNothingElse_IsValid()
    {
        var errors = TimeOffRules.Validate(Day(25), Today, []);

        Assert.Empty(errors);
    }

    [Fact]
    public void Today_IsValid()
    {
        var errors = TimeOffRules.Validate(Day(23), Today, []);

        Assert.Empty(errors);
    }

    [Fact]
    public void DayInThePast_IsAnError()
    {
        var errors = TimeOffRules.Validate(Day(22), Today, []);

        Assert.Single(errors);
    }

    [Fact]
    public void SameDayAsAnExistingRequest_IsAnError()
    {
        var errors = TimeOffRules.Validate(Day(25), Today, [Range(25, 25)]);

        Assert.Single(errors);
    }

    [Fact]
    public void DayInsideAnOlderMultiDayRequest_IsAnError()
    {
        var errors = TimeOffRules.Validate(Day(26), Today, [Range(24, 28)]);

        Assert.Single(errors);
    }

    [Fact]
    public void FirstAndLastDayOfAnOlderMultiDayRequest_AreErrors()
    {
        Assert.Single(TimeOffRules.Validate(Day(24), Today, [Range(24, 28)]));
        Assert.Single(TimeOffRules.Validate(Day(28), Today, [Range(24, 28)]));
    }

    [Fact]
    public void DayBeforeOrAfterAnExistingRequest_IsValid()
    {
        Assert.Empty(TimeOffRules.Validate(Day(23), Today, [Range(24, 28)]));
        Assert.Empty(TimeOffRules.Validate(Day(29), Today, [Range(24, 28)]));
    }

    [Fact]
    public void PastDayThatAlsoOverlaps_ReportsBothProblems()
    {
        var errors = TimeOffRules.Validate(Day(22), Today, [Range(20, 25)]);

        Assert.Equal(2, errors.Count);
    }
}

public class ShortNoticeTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 14, 0, 0);

    private static DateOnly Day(int day) => new(2026, 11, day);

    private static ShiftSpan Shift(int day, int startHour, int endHour) =>
        ShiftSpan.From(Day(day), new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    [Fact]
    public void ShiftLaterToday_CoveredByLeave_IsShortNotice()
    {
        var result = TimeOffRules.IsShortNotice([Shift(23, 19, 23)], Day(23), Day(23), Now);

        Assert.True(result);
    }

    [Fact]
    public void ShiftTomorrowWithin24Hours_IsShortNotice()
    {
        var result = TimeOffRules.IsShortNotice([Shift(24, 13, 21)], Day(24), Day(24), Now);

        Assert.True(result);
    }

    [Fact]
    public void ShiftStartingExactly24HoursAway_IsShortNotice()
    {
        var result = TimeOffRules.IsShortNotice([Shift(24, 14, 22)], Day(24), Day(24), Now);

        Assert.True(result);
    }

    [Fact]
    public void ShiftMoreThan24HoursAway_IsNotShortNotice()
    {
        var result = TimeOffRules.IsShortNotice([Shift(24, 15, 22)], Day(24), Day(24), Now);

        Assert.False(result);
    }

    [Fact]
    public void ShiftThatAlreadyEnded_IsNotShortNotice()
    {
        var result = TimeOffRules.IsShortNotice([Shift(23, 8, 12)], Day(23), Day(23), Now);

        Assert.False(result);
    }

    [Fact]
    public void ShiftInProgress_IsShortNotice()
    {
        var result = TimeOffRules.IsShortNotice([Shift(23, 12, 20)], Day(23), Day(23), Now);

        Assert.True(result);
    }

    [Fact]
    public void LeaveThatDoesNotCoverTheShift_IsNotShortNotice()
    {
        var result = TimeOffRules.IsShortNotice([Shift(23, 19, 23)], Day(25), Day(26), Now);

        Assert.False(result);
    }

    [Fact]
    public void OvernightShiftSpillingIntoTheLeaveDay_IsShortNotice()
    {
        var overnight = ShiftSpan.From(Day(23), new TimeOnly(22, 0), new TimeOnly(2, 0));

        var result = TimeOffRules.IsShortNotice([overnight], Day(24), Day(24), Now);

        Assert.True(result);
    }

    [Fact]
    public void NoShifts_IsNotShortNotice()
    {
        var result = TimeOffRules.IsShortNotice([], Day(23), Day(25), Now);

        Assert.False(result);
    }
}

public class ShiftSpanTests
{
    [Fact]
    public void DaytimeShift_EndsTheSameDay()
    {
        var span = ShiftSpan.From(new DateOnly(2026, 11, 23), new TimeOnly(9, 0), new TimeOnly(17, 0));

        Assert.Equal(new DateTime(2026, 11, 23, 9, 0, 0), span.Start);
        Assert.Equal(new DateTime(2026, 11, 23, 17, 0, 0), span.End);
    }

    [Fact]
    public void OvernightShift_EndsTheNextDay()
    {
        var span = ShiftSpan.From(new DateOnly(2026, 11, 23), new TimeOnly(22, 0), new TimeOnly(2, 0));

        Assert.Equal(new DateTime(2026, 11, 23, 22, 0, 0), span.Start);
        Assert.Equal(new DateTime(2026, 11, 24, 2, 0, 0), span.End);
    }
}
