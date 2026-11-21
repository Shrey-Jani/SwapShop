using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Entities;
using SwapShop.Core.Enums;
using SwapShop.Core.Models;
using SwapShop.Core.Rules;
using SwapShop.Core.Services;
using SwapShop.Core.Utilities;

namespace SwapShop.Data;

public class ConflictService(IDbContextFactory<ApplicationDbContext> dbFactory) : IConflictService
{
    public async Task<IReadOnlyList<Conflict>> CheckAsync(string? userId, ShiftSlot slot)
    {
        if (userId is null)
        {
            return [];
        }

        var span = slot.Span;
        var from = DateOnly.FromDateTime(span.Start).AddDays(-1);
        var to = DateOnly.FromDateTime(span.End).AddDays(1);

        await using var db = await dbFactory.CreateDbContextAsync();

        var data = await LoadAsync(db, [userId], from, to);

        return Evaluate(data, userId, slot);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<Conflict>>> GetWeekConflictsAsync(DateOnly weekStart)
    {
        var week = WeekHelper.StartOfWeek(weekStart);
        var weekEnd = week.AddDays(6);

        await using var db = await dbFactory.CreateDbContextAsync();

        var userIds = await db.Shifts
            .AsNoTracking()
            .Where(s => s.AssignedUserId != null && s.Date >= week && s.Date <= weekEnd)
            .Select(s => s.AssignedUserId!)
            .Distinct()
            .ToListAsync();

        var result = new Dictionary<int, IReadOnlyList<Conflict>>();

        if (userIds.Count == 0)
        {
            return result;
        }

        var data = await LoadAsync(db, userIds, week.AddDays(-1), weekEnd.AddDays(1));

        foreach (var (userId, slots) in data.Shifts)
        {
            foreach (var slot in slots.Where(s => s.Date >= week && s.Date <= weekEnd))
            {
                var conflicts = Evaluate(data, userId, slot);

                if (conflicts.Count > 0 && slot.Id is int id)
                {
                    result[id] = conflicts;
                }
            }
        }

        return result;
    }

    private static IReadOnlyList<Conflict> Evaluate(ConflictData data, string userId, ShiftSlot slot)
    {
        var others = data.Shifts.GetValueOrDefault(userId) ?? [];
        var leave = data.Leave.GetValueOrDefault(userId) ?? [];
        var rows = data.Availability.GetValueOrDefault(userId) ?? [];

        var availabilityByWeek = WeeksTouched(slot)
            .ToDictionary(week => week, week => ResolveWeek(rows, week));

        return ConflictChecker.Check(slot, others, availabilityByWeek, leave);
    }

    private static WeekAvailability ResolveWeek(IReadOnlyList<Availability> rows, DateOnly week)
    {
        var standing = rows.Where(r => r.WeekStart is null).Select(ToWindow).ToList();
        var custom = rows.Where(r => r.WeekStart == week).Select(ToWindow).ToList();

        return AvailabilityResolver.Resolve(standing, custom);
    }

    private static IEnumerable<DateOnly> WeeksTouched(ShiftSlot slot)
    {
        var span = slot.Span;
        var firstDay = DateOnly.FromDateTime(span.Start);
        var lastDay = span.End.TimeOfDay == TimeSpan.Zero
            ? DateOnly.FromDateTime(span.End).AddDays(-1)
            : DateOnly.FromDateTime(span.End);

        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            yield return WeekHelper.StartOfWeek(day);
        }
    }

    private static AvailabilityWindow ToWindow(Availability row) =>
        new(row.DayOfWeek, row.StartTime, row.EndTime);

    private static async Task<ConflictData> LoadAsync(
        ApplicationDbContext db, IReadOnlyCollection<string> userIds, DateOnly from, DateOnly to)
    {
        var fromWeek = WeekHelper.StartOfWeek(from);
        var toWeek = WeekHelper.StartOfWeek(to);

        var shifts = await db.Shifts
            .AsNoTracking()
            .Where(s => s.AssignedUserId != null
                        && userIds.Contains(s.AssignedUserId)
                        && s.Date >= from
                        && s.Date <= to)
            .ToListAsync();

        var availability = await db.Availabilities
            .AsNoTracking()
            .Where(a => userIds.Contains(a.UserId)
                        && (a.WeekStart == null || (a.WeekStart >= fromWeek && a.WeekStart <= toWeek)))
            .ToListAsync();

        var leave = await db.TimeOffRequests
            .AsNoTracking()
            .Where(r => userIds.Contains(r.UserId)
                        && r.Status == TimeOffStatus.Approved
                        && r.StartDate <= to
                        && r.EndDate >= from)
            .ToListAsync();

        return new ConflictData(
            shifts
                .GroupBy(s => s.AssignedUserId!)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<ShiftSlot>)g
                        .Select(s => new ShiftSlot(s.Id, s.Date, s.StartTime, s.EndTime))
                        .ToList()),
            availability
                .GroupBy(a => a.UserId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Availability>)g.ToList()),
            leave
                .GroupBy(r => r.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<DateRange>)g.Select(r => new DateRange(r.StartDate, r.EndDate)).ToList()));
    }

    private sealed record ConflictData(
        Dictionary<string, IReadOnlyList<ShiftSlot>> Shifts,
        Dictionary<string, IReadOnlyList<Availability>> Availability,
        Dictionary<string, IReadOnlyList<DateRange>> Leave);
}
