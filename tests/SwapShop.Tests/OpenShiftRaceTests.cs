using SwapShop.Core.Models;
using SwapShop.Core.Services;
using SwapShop.Data;
using SwapShop.Tests.Infrastructure;

namespace SwapShop.Tests;

public sealed class OpenShiftRaceTests : IDisposable
{
    private readonly TestDatabase db = new();
    private readonly FixedTimeProvider time = new(new DateTime(2026, 11, 23, 8, 0, 0));

    public OpenShiftRaceTests()
    {
        db.AddUser("u1", "Una", "One");
        db.AddUser("u2", "Dee", "Two");
    }

    public void Dispose() => db.Dispose();

    private static DateOnly D(int day) => new(2026, 12, day);

    [Fact]
    public async Task TwoEmployeesClaimingTheSameShiftAtTheSameMoment_ExactlyOneWins()
    {
        var shift = db.AddShift(D(1), new TimeOnly(9, 0), new TimeOnly(17, 0));

        // Both claimants are held right after "the shift looks free" and released together,
        // so both reach the final assignment step at the same time.
        var gate = new GatedConflictService(new ConflictService(db.Factory), participants: 2);
        var service = new OpenShiftService(db.Factory, gate, time);

        var results = await Task.WhenAll(
            Task.Run(() => service.ClaimAsync("u1", shift)),
            Task.Run(() => service.ClaimAsync("u2", shift)));

        Assert.Equal(1, results.Count(r => r.Succeeded));

        var loser = Assert.Single(results, r => !r.Succeeded);
        Assert.Contains("just taken", loser.Errors[0]);

        var winner = results[0].Succeeded ? "u1" : "u2";
        Assert.Equal(winner, db.GetAssignedUserId(shift));
    }

    [Fact]
    public async Task TheSameEmployeeClickingTwice_TakesTheShiftOnlyOnce()
    {
        var shift = db.AddShift(D(1), new TimeOnly(9, 0), new TimeOnly(17, 0));

        var gate = new GatedConflictService(new ConflictService(db.Factory), participants: 2);
        var service = new OpenShiftService(db.Factory, gate, time);

        var results = await Task.WhenAll(
            Task.Run(() => service.ClaimAsync("u1", shift)),
            Task.Run(() => service.ClaimAsync("u1", shift)));

        Assert.Equal(1, results.Count(r => r.Succeeded));
        Assert.Equal("u1", db.GetAssignedUserId(shift));
    }

    [Fact]
    public async Task ManyShiftsClaimedByTwoEmployeesAtOnce_EveryShiftHasExactlyOneWinner()
    {
        var service = new OpenShiftService(db.Factory, new ConflictService(db.Factory), time);

        var shifts = Enumerable.Range(1, 20)
            .Select(day => db.AddShift(D(day), new TimeOnly(9, 0), new TimeOnly(17, 0)))
            .ToList();

        var attempts = shifts
            .SelectMany(id => new[] { "u1", "u2" }.Select(user => (id, user)))
            .Select(a => Task.Run(async () => (a.id, a.user, result: await service.ClaimAsync(a.user, a.id))))
            .ToList();

        var outcomes = await Task.WhenAll(attempts);

        foreach (var id in shifts)
        {
            var forShift = outcomes.Where(o => o.id == id).ToList();

            var winner = Assert.Single(forShift, o => o.result.Succeeded);
            Assert.Equal(winner.user, db.GetAssignedUserId(id));
        }
    }

    private sealed class GatedConflictService(IConflictService inner, int participants) : IConflictService
    {
        private readonly Barrier barrier = new(participants);

        public async Task<IReadOnlyList<Conflict>> CheckAsync(string? userId, ShiftSlot slot)
        {
            var conflicts = await inner.CheckAsync(userId, slot);

            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("The second claimant never arrived at the gate.");
            }

            return conflicts;
        }

        public Task<IReadOnlyDictionary<int, IReadOnlyList<Conflict>>> GetWeekConflictsAsync(DateOnly weekStart) =>
            inner.GetWeekConflictsAsync(weekStart);
    }
}
