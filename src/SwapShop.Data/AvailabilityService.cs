using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Entities;
using SwapShop.Core.Models;
using SwapShop.Core.Rules;
using SwapShop.Core.Services;
using SwapShop.Core.Utilities;

namespace SwapShop.Data;

public class AvailabilityService(IDbContextFactory<ApplicationDbContext> dbFactory) : IAvailabilityService
{
    public Task<IReadOnlyList<AvailabilityWindow>> GetStandingAsync(string userId) =>
        ReadAsync(userId, null);

    public Task<ServiceResult> SaveStandingAsync(string userId, IReadOnlyList<AvailabilityWindow> windows) =>
        ReplaceAsync(userId, null, windows);

    public Task<IReadOnlyList<AvailabilityWindow>> GetWeekOverrideAsync(string userId, DateOnly weekStart) =>
        ReadAsync(userId, WeekHelper.StartOfWeek(weekStart));

    public Task<ServiceResult> SaveWeekOverrideAsync(
        string userId, DateOnly weekStart, IReadOnlyList<AvailabilityWindow> windows) =>
        ReplaceAsync(userId, WeekHelper.StartOfWeek(weekStart), windows);

    public async Task<WeekAvailability> GetEffectiveAsync(string userId, DateOnly weekStart)
    {
        var standing = await ReadAsync(userId, null);
        var custom = await ReadAsync(userId, WeekHelper.StartOfWeek(weekStart));

        return AvailabilityResolver.Resolve(standing, custom);
    }

    public async Task<IReadOnlyList<EmployeeWeekAvailability>> GetOverviewAsync(DateOnly weekStart)
    {
        var week = WeekHelper.StartOfWeek(weekStart);

        await using var db = await dbFactory.CreateDbContextAsync();

        var employees = await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToListAsync();

        var rows = await db.Availabilities
            .AsNoTracking()
            .Where(a => a.WeekStart == null || a.WeekStart == week)
            .ToListAsync();

        return employees
            .Select(employee =>
            {
                var standing = ToWindows(rows.Where(r => r.UserId == employee.Id && r.WeekStart == null));
                var custom = ToWindows(rows.Where(r => r.UserId == employee.Id && r.WeekStart == week));
                var effective = AvailabilityResolver.Resolve(standing, custom);

                return new EmployeeWeekAvailability(employee.Id, employee.Name, effective.Source, effective.Windows);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<AvailabilityWindow>> ReadAsync(string userId, DateOnly? weekStart)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var rows = await db.Availabilities
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.WeekStart == weekStart)
            .ToListAsync();

        return ToWindows(rows);
    }

    private async Task<ServiceResult> ReplaceAsync(
        string userId, DateOnly? weekStart, IReadOnlyList<AvailabilityWindow> windows)
    {
        var errors = AvailabilityRules.Validate(windows);
        if (errors.Count > 0)
        {
            return ServiceResult.Failure(errors);
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            return ServiceResult.Failure("Employee not found.");
        }

        var existing = await db.Availabilities
            .Where(a => a.UserId == userId && a.WeekStart == weekStart)
            .ToListAsync();

        db.Availabilities.RemoveRange(existing);

        db.Availabilities.AddRange(windows.Select(w => new Availability
        {
            UserId = userId,
            WeekStart = weekStart,
            DayOfWeek = w.Day,
            StartTime = w.Start,
            EndTime = w.End
        }));

        await db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    private static List<AvailabilityWindow> ToWindows(IEnumerable<Availability> rows) =>
        rows
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .Select(a => new AvailabilityWindow(a.DayOfWeek, a.StartTime, a.EndTime))
            .ToList();
}
