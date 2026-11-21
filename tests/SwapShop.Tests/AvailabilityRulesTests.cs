using SwapShop.Core.Models;
using SwapShop.Core.Rules;

namespace SwapShop.Tests;

public class AvailabilityRulesTests
{
    private static AvailabilityWindow Window(DayOfWeek day, int startHour, int endHour) =>
        new(day, new TimeOnly(startHour, 0), new TimeOnly(endHour, 0));

    [Fact]
    public void SingleValidWindow_HasNoErrors()
    {
        var errors = AvailabilityRules.Validate([Window(DayOfWeek.Monday, 9, 17)]);

        Assert.Empty(errors);
    }

    [Fact]
    public void EmptyList_HasNoErrors()
    {
        var errors = AvailabilityRules.Validate([]);

        Assert.Empty(errors);
    }

    [Fact]
    public void EndBeforeStart_IsAnError()
    {
        var errors = AvailabilityRules.Validate([Window(DayOfWeek.Monday, 17, 9)]);

        Assert.Single(errors);
    }

    [Fact]
    public void EndEqualToStart_IsAnError()
    {
        var errors = AvailabilityRules.Validate([Window(DayOfWeek.Monday, 9, 9)]);

        Assert.Single(errors);
    }

    [Fact]
    public void OverlappingWindowsOnSameDay_IsAnError()
    {
        var errors = AvailabilityRules.Validate(
        [
            Window(DayOfWeek.Tuesday, 9, 13),
            Window(DayOfWeek.Tuesday, 12, 17)
        ]);

        Assert.Single(errors);
    }

    [Fact]
    public void NestedWindows_AreAllReported()
    {
        // 09-17 contains both 10-11 and 12-13, so both overlap it.
        // Comparing only neighbours would miss the 12-13 window.
        var errors = AvailabilityRules.Validate(
        [
            Window(DayOfWeek.Friday, 9, 17),
            Window(DayOfWeek.Friday, 10, 11),
            Window(DayOfWeek.Friday, 12, 13)
        ]);

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void TouchingWindows_AreValid()
    {
        var errors = AvailabilityRules.Validate(
        [
            Window(DayOfWeek.Wednesday, 9, 12),
            Window(DayOfWeek.Wednesday, 12, 17)
        ]);

        Assert.Empty(errors);
    }

    [Fact]
    public void SameHoursOnDifferentDays_AreValid()
    {
        var errors = AvailabilityRules.Validate(
        [
            Window(DayOfWeek.Monday, 9, 17),
            Window(DayOfWeek.Tuesday, 9, 17)
        ]);

        Assert.Empty(errors);
    }
}
