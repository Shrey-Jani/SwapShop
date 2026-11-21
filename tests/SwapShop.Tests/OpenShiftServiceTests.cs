using SwapShop.Core.Enums;
using SwapShop.Data;
using SwapShop.Tests.Infrastructure;

namespace SwapShop.Tests;

public sealed class OpenShiftServiceTests : IDisposable
{
    private readonly TestDatabase db = new();
    private readonly OpenShiftService service;

    public OpenShiftServiceTests()
    {
        // "Now" is Monday 23 Nov 2026, 08:00.
        var time = new FixedTimeProvider(new DateTime(2026, 11, 23, 8, 0, 0));
        service = new OpenShiftService(db.Factory, new ConflictService(db.Factory), time);

        db.AddUser("u1", "Una", "One");
        db.AddUser("u2", "Dee", "Two");
        db.AddUser("u3", "Cass", "Cashier", Position.Cashier);
        db.AddUser("u4", "Old", "Gone", active: false);
    }

    public void Dispose() => db.Dispose();

    private static DateOnly D(int day) => new(2026, 11, day);

    private static TimeOnly T(int hour) => new(hour, 0);

    [Fact]
    public async Task OpenShifts_ListOnlyPublishedUnassignedUpcomingShiftsForTheEmployeesPosition()
    {
        var good = db.AddShift(D(24), T(9), T(17));
        db.AddShift(D(24), T(9), T(17), published: false);
        db.AddShift(D(25), T(9), T(17), "u2");
        db.AddShift(D(24), T(9), T(17), position: Position.Cashier);
        db.AddShift(D(23), T(7), T(15));
        db.AddShift(D(30), T(9), T(17));

        var shifts = await service.GetOpenShiftsAsync("u1", D(23));

        Assert.Equal([good], shifts.Select(s => s.Shift.Id));
        Assert.True(shifts[0].CanTake);
    }

    [Fact]
    public async Task OpenShifts_AreOrderedByDateThenStartTime()
    {
        var wedMorning = db.AddShift(D(25), T(8), T(12));
        var tueEvening = db.AddShift(D(24), T(17), T(22));
        var tueMorning = db.AddShift(D(24), T(8), T(12));

        var shifts = await service.GetOpenShiftsAsync("u1", D(23));

        Assert.Equal([tueMorning, tueEvening, wedMorning], shifts.Select(s => s.Shift.Id));
    }

    [Fact]
    public async Task OpenShifts_ThatConflict_AreListedButCannotBeTaken()
    {
        db.AddShift(D(24), T(9), T(17), "u1");
        var open = db.AddShift(D(24), T(16), T(20));

        var item = Assert.Single(await service.GetOpenShiftsAsync("u1", D(23)));

        Assert.Equal(open, item.Shift.Id);
        Assert.False(item.CanTake);
        Assert.Equal(ConflictType.Overlap, Assert.Single(item.Conflicts).Type);
    }

    [Fact]
    public async Task OpenShifts_ForAnInactiveOrUnknownEmployee_AreEmpty()
    {
        db.AddShift(D(24), T(9), T(17));

        Assert.Empty(await service.GetOpenShiftsAsync("u4", D(23)));
        Assert.Empty(await service.GetOpenShiftsAsync("nobody", D(23)));
    }

    [Fact]
    public async Task Claim_AssignsTheShiftToTheEmployee()
    {
        var shift = db.AddShift(D(24), T(9), T(17));

        var result = await service.ClaimAsync("u1", shift);

        Assert.True(result.Succeeded);
        Assert.Equal("u1", db.GetAssignedUserId(shift));
    }

    [Fact]
    public async Task Claim_OfADraftShift_Fails_AndLeavesItOpen()
    {
        var shift = db.AddShift(D(24), T(9), T(17), published: false);

        var result = await service.ClaimAsync("u1", shift);

        Assert.False(result.Succeeded);
        Assert.Null(db.GetAssignedUserId(shift));
    }

    [Fact]
    public async Task Claim_OfAShiftAlreadyTaken_Fails_AndKeepsTheFirstEmployee()
    {
        var shift = db.AddShift(D(24), T(9), T(17));

        Assert.True((await service.ClaimAsync("u1", shift)).Succeeded);
        Assert.False((await service.ClaimAsync("u2", shift)).Succeeded);

        Assert.Equal("u1", db.GetAssignedUserId(shift));
    }

    [Fact]
    public async Task Claim_OfAShiftForAnotherPosition_Fails()
    {
        var shift = db.AddShift(D(24), T(9), T(17), position: Position.Cook);

        var result = await service.ClaimAsync("u3", shift);

        Assert.False(result.Succeeded);
        Assert.Null(db.GetAssignedUserId(shift));
    }

    [Fact]
    public async Task Claim_OfAShiftThatAlreadyStarted_Fails()
    {
        var shift = db.AddShift(D(23), T(7), T(15));

        Assert.False((await service.ClaimAsync("u1", shift)).Succeeded);
        Assert.Null(db.GetAssignedUserId(shift));
    }

    [Fact]
    public async Task Claim_ByAnInactiveOrUnknownEmployee_Fails()
    {
        var shift = db.AddShift(D(24), T(9), T(17));

        Assert.False((await service.ClaimAsync("u4", shift)).Succeeded);
        Assert.False((await service.ClaimAsync("nobody", shift)).Succeeded);
        Assert.Null(db.GetAssignedUserId(shift));
    }

    [Fact]
    public async Task Claim_OfAShiftThatDoesNotExist_Fails()
    {
        Assert.False((await service.ClaimAsync("u1", 9999)).Succeeded);
    }

    [Fact]
    public async Task Claim_IsBlockedByAnOverlappingShift()
    {
        db.AddShift(D(24), T(9), T(17), "u1");
        var open = db.AddShift(D(24), T(16), T(20));

        var result = await service.ClaimAsync("u1", open);

        Assert.False(result.Succeeded);
        Assert.Contains("Overlaps", result.Errors[0]);
        Assert.Null(db.GetAssignedUserId(open));
    }

    [Fact]
    public async Task Claim_IsBlockedByAvailability()
    {
        db.AddAvailability("u1", DayOfWeek.Tuesday, T(9), T(12));
        var open = db.AddShift(D(24), T(9), T(17));

        var result = await service.ClaimAsync("u1", open);

        Assert.False(result.Succeeded);
        Assert.Contains("available", result.Errors[0]);
        Assert.Null(db.GetAssignedUserId(open));
    }

    [Fact]
    public async Task Claim_IsBlockedByApprovedTimeOff_ButNotByPendingTimeOff()
    {
        db.AddTimeOff("u1", D(24), D(24), TimeOffStatus.Approved);
        db.AddTimeOff("u2", D(24), D(24), TimeOffStatus.Pending);
        var open = db.AddShift(D(24), T(9), T(17));

        Assert.False((await service.ClaimAsync("u1", open)).Succeeded);
        Assert.Null(db.GetAssignedUserId(open));

        Assert.True((await service.ClaimAsync("u2", open)).Succeeded);
        Assert.Equal("u2", db.GetAssignedUserId(open));
    }

    [Fact]
    public async Task Claim_OfTwoOverlappingOpenShifts_BlocksTheSecondOne()
    {
        var first = db.AddShift(D(24), T(9), T(17));
        var second = db.AddShift(D(24), T(16), T(20));

        Assert.True((await service.ClaimAsync("u1", first)).Succeeded);
        Assert.False((await service.ClaimAsync("u1", second)).Succeeded);

        Assert.Null(db.GetAssignedUserId(second));
    }
}
