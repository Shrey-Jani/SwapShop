using SwapShop.Core.Enums;
using SwapShop.Core.Models;
using SwapShop.Core.Rules;

namespace SwapShop.Tests;

public class OpenShiftRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 8, 0, 0);

    private static ShiftDto Shift(
        Position position = Position.Cook,
        bool published = true,
        string? assignedTo = null,
        int day = 24,
        int startHour = 9,
        int endHour = 17) =>
        new(1, new DateOnly(2026, 11, day), new TimeOnly(startHour, 0), new TimeOnly(endHour, 0),
            position, assignedTo, null, null, published);

    [Fact]
    public void PublishedOpenShiftForTheSamePosition_CanBeClaimed()
    {
        var errors = OpenShiftRules.CanClaim(true, Position.Cook, Shift(), Now);

        Assert.Empty(errors);
    }

    [Fact]
    public void UnpublishedShift_IsReportedAsNotAvailable_AndNothingElse()
    {
        var errors = OpenShiftRules.CanClaim(
            false, Position.Cashier, Shift(published: false, assignedTo: "someone"), Now);

        Assert.Equal(["This shift is not available."], errors);
    }

    [Fact]
    public void InactiveEmployee_CannotClaim()
    {
        var errors = OpenShiftRules.CanClaim(false, Position.Cook, Shift(), Now);

        Assert.Single(errors);
    }

    [Fact]
    public void ShiftThatIsAlreadyAssigned_CannotBeClaimed()
    {
        var errors = OpenShiftRules.CanClaim(true, Position.Cook, Shift(assignedTo: "u2"), Now);

        Assert.Single(errors);
        Assert.Contains("already been taken", errors[0]);
    }

    [Fact]
    public void ShiftForADifferentPosition_CannotBeClaimed()
    {
        var errors = OpenShiftRules.CanClaim(true, Position.Cashier, Shift(position: Position.Cook), Now);

        Assert.Single(errors);
        Assert.Contains("different position", errors[0]);
    }

    [Fact]
    public void ShiftThatAlreadyStarted_CannotBeClaimed()
    {
        var errors = OpenShiftRules.CanClaim(true, Position.Cook, Shift(day: 23, startHour: 7, endHour: 15), Now);

        Assert.Single(errors);
        Assert.Contains("already started", errors[0]);
    }

    [Fact]
    public void ShiftStartingExactlyNow_CountsAsStarted()
    {
        var errors = OpenShiftRules.CanClaim(true, Position.Cook, Shift(day: 23, startHour: 8, endHour: 16), Now);

        Assert.Single(errors);
    }

    [Fact]
    public void ShiftStartingLaterToday_CanBeClaimed()
    {
        var errors = OpenShiftRules.CanClaim(true, Position.Cook, Shift(day: 23, startHour: 9, endHour: 17), Now);

        Assert.Empty(errors);
    }

    [Fact]
    public void SeveralProblems_AreAllReported()
    {
        var errors = OpenShiftRules.CanClaim(
            false, Position.Cashier, Shift(assignedTo: "u2", day: 23, startHour: 7, endHour: 15), Now);

        Assert.Equal(4, errors.Count);
    }
}
