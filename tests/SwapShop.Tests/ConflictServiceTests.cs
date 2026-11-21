using SwapShop.Core.Enums;
using SwapShop.Core.Models;
using SwapShop.Data;
using SwapShop.Tests.Infrastructure;

namespace SwapShop.Tests;

public sealed class ConflictServiceTests : IDisposable
{
    private readonly TestDatabase db = new();
    private readonly ConflictService service;

    public ConflictServiceTests()
    {
        service = new ConflictService(db.Factory);
        db.AddUser("u1", "Una", "One");
        db.AddUser("u2", "Dee", "Two");
    }

    public void Dispose() => db.Dispose();

    // Monday 23 Nov 2026 starts week A, Monday 30 Nov 2026 starts week B.
    private static DateOnly D(int day) => new(2026, 11, day);

    private static TimeOnly T(int hour, int minute = 0) => new(hour, minute);

    private static ShiftSlot Slot(int? id, int day, int startHour, int endHour) =>
        new(id, D(day), T(startHour), T(endHour));

    [Fact]
    public async Task OpenShift_WithNoUser_HasNoConflicts()
    {
        var conflicts = await service.CheckAsync(null, Slot(null, 23, 9, 17));

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task OverlapWithAnotherShiftOfTheSameUser_IsReported_EvenWhenTheOtherShiftIsADraft()
    {
        var otherId = db.AddShift(D(23), T(9), T(17), "u1", published: false);

        var conflict = Assert.Single(await service.CheckAsync("u1", Slot(null, 23, 16, 20)));

        Assert.Equal(ConflictType.Overlap, conflict.Type);
        Assert.Equal(otherId, conflict.RelatedShiftId);
    }

    [Fact]
    public async Task ShiftsOfOtherUsers_AreIgnored()
    {
        db.AddShift(D(23), T(9), T(17), "u2");

        Assert.Empty(await service.CheckAsync("u1", Slot(null, 23, 9, 17)));
    }

    [Fact]
    public async Task EditingAShift_DoesNotConflictWithItself()
    {
        var id = db.AddShift(D(23), T(9), T(17), "u1");

        Assert.Empty(await service.CheckAsync("u1", Slot(id, 23, 9, 17)));
    }

    [Fact]
    public async Task OvernightShiftFromThePreviousDay_IsLoadedAndOverlaps()
    {
        db.AddShift(D(23), T(22), T(2), "u1");

        var conflict = Assert.Single(await service.CheckAsync("u1", Slot(null, 24, 1, 5)));

        Assert.Equal(ConflictType.Overlap, conflict.Type);
    }

    [Fact]
    public async Task NoAvailabilitySet_ProducesNoConflict()
    {
        Assert.Empty(await service.CheckAsync("u1", Slot(null, 23, 9, 17)));
    }

    [Fact]
    public async Task StandingAvailability_IsAppliedToAnyWeek()
    {
        db.AddAvailability("u1", DayOfWeek.Monday, T(9), T(17));

        Assert.Empty(await service.CheckAsync("u1", Slot(null, 23, 9, 17)));
        Assert.Empty(await service.CheckAsync("u1", Slot(null, 30, 9, 17)));

        var conflict = Assert.Single(await service.CheckAsync("u1", Slot(null, 23, 8, 17)));
        Assert.Equal(ConflictType.OutsideAvailability, conflict.Type);
    }

    [Fact]
    public async Task CustomWeekAvailability_ReplacesTheStandingPatternForThatWeekOnly()
    {
        db.AddAvailability("u1", DayOfWeek.Monday, T(9), T(17));
        db.AddAvailability("u1", DayOfWeek.Monday, T(12), T(14), weekStart: D(23));

        Assert.Single(await service.CheckAsync("u1", Slot(null, 23, 9, 17)));
        Assert.Empty(await service.CheckAsync("u1", Slot(null, 30, 9, 17)));
    }

    [Fact]
    public async Task AvailabilityOfOtherUsers_IsIgnored()
    {
        db.AddAvailability("u2", DayOfWeek.Monday, T(9), T(10));

        Assert.Empty(await service.CheckAsync("u1", Slot(null, 23, 9, 17)));
    }

    [Fact]
    public async Task ShiftCrossingIntoNextWeek_UsesNextWeeksCustomAvailability()
    {
        db.AddAvailability("u1", DayOfWeek.Sunday, T(18), T(23, 59));
        db.AddAvailability("u1", DayOfWeek.Monday, T(0), T(6));

        // Sunday 29 Nov 22:00 to Monday 30 Nov 02:00 is fine with the standing pattern.
        Assert.Empty(await service.CheckAsync("u1", Slot(null, 29, 22, 2)));

        db.AddAvailability("u1", DayOfWeek.Monday, T(8), T(16), weekStart: D(30));

        // A custom week B replaces it, and the Monday part is now outside it.
        var conflict = Assert.Single(await service.CheckAsync("u1", Slot(null, 29, 22, 2)));
        Assert.Contains("Monday", conflict.Message);
    }

    [Fact]
    public async Task ApprovedTimeOff_IsReported()
    {
        db.AddTimeOff("u1", D(24), D(24), TimeOffStatus.Approved);

        var conflict = Assert.Single(await service.CheckAsync("u1", Slot(null, 24, 9, 17)));

        Assert.Equal(ConflictType.TimeOff, conflict.Type);
    }

    [Theory]
    [InlineData(TimeOffStatus.Pending)]
    [InlineData(TimeOffStatus.Denied)]
    [InlineData(TimeOffStatus.Cancelled)]
    public async Task TimeOffThatIsNotApproved_IsIgnored(TimeOffStatus status)
    {
        db.AddTimeOff("u1", D(24), D(24), status);

        Assert.Empty(await service.CheckAsync("u1", Slot(null, 24, 9, 17)));
    }

    [Fact]
    public async Task TimeOffOfOtherUsers_IsIgnored()
    {
        db.AddTimeOff("u2", D(24), D(24), TimeOffStatus.Approved);

        Assert.Empty(await service.CheckAsync("u1", Slot(null, 24, 9, 17)));
    }

    [Fact]
    public async Task WeekConflicts_FlagBothOverlappingShifts_AndNothingElse()
    {
        var a = db.AddShift(D(23), T(9), T(17), "u1");
        var b = db.AddShift(D(23), T(16), T(20), "u1");
        var fine = db.AddShift(D(24), T(9), T(17), "u1");
        var open = db.AddShift(D(23), T(9), T(17));

        var conflicts = await service.GetWeekConflictsAsync(D(23));

        Assert.Equal([a, b], conflicts.Keys.Order());
        Assert.DoesNotContain(fine, conflicts.Keys);
        Assert.DoesNotContain(open, conflicts.Keys);
        Assert.All(conflicts.Values, list => Assert.Equal(ConflictType.Overlap, Assert.Single(list).Type));
    }

    [Fact]
    public async Task WeekConflicts_OnlyIncludeShiftsOfTheRequestedWeek()
    {
        db.AddShift(D(30), T(9), T(17), "u1");
        db.AddShift(D(30), T(16), T(20), "u1");

        Assert.Empty(await service.GetWeekConflictsAsync(D(23)));
        Assert.Equal(2, (await service.GetWeekConflictsAsync(D(30))).Count);
    }

    [Fact]
    public async Task WeekConflicts_AcceptAnyDayOfTheWeek()
    {
        db.AddShift(D(23), T(9), T(17), "u1");
        db.AddShift(D(23), T(16), T(20), "u1");

        var fromMonday = await service.GetWeekConflictsAsync(D(23));
        var fromThursday = await service.GetWeekConflictsAsync(D(26));

        Assert.Equal(fromMonday.Keys.Order(), fromThursday.Keys.Order());
    }

    [Fact]
    public async Task WeekConflicts_IncludeAvailabilityAndTimeOffProblems()
    {
        var tuesday = db.AddShift(D(24), T(9), T(17), "u1");
        db.AddTimeOff("u1", D(24), D(24), TimeOffStatus.Approved);
        db.AddAvailability("u2", DayOfWeek.Monday, T(9), T(10));
        var wednesday = db.AddShift(D(25), T(9), T(17), "u2");

        var conflicts = await service.GetWeekConflictsAsync(D(23));

        Assert.Equal(ConflictType.TimeOff, Assert.Single(conflicts[tuesday]).Type);
        Assert.Equal(ConflictType.OutsideAvailability, Assert.Single(conflicts[wednesday]).Type);
    }

    [Fact]
    public async Task WeekConflicts_WithNoShifts_IsEmpty()
    {
        Assert.Empty(await service.GetWeekConflictsAsync(D(23)));
    }
}
