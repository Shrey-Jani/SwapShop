using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Entities;
using SwapShop.Core.Models;
using SwapShop.Core.Rules;
using SwapShop.Core.Services;
using SwapShop.Core.Utilities;

namespace SwapShop.Data;

public class OpenShiftService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IConflictService conflictService,
    TimeProvider timeProvider) : IOpenShiftService
{
    public async Task<IReadOnlyList<OpenShiftDto>> GetOpenShiftsAsync(string userId, DateOnly weekStart)
    {
        var week = WeekHelper.StartOfWeek(weekStart);
        var weekEnd = week.AddDays(6);
        var now = timeProvider.GetLocalNow().DateTime;

        await using var db = await dbFactory.CreateDbContextAsync();

        var employee = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new { u.Position })
            .FirstOrDefaultAsync();

        if (employee is null)
        {
            return [];
        }

        var isManager = PositionRules.IsManager(employee.Position, await db.IsInManagerRoleAsync(userId));
        var workable = PositionRules.WorkablePositions(employee.Position, isManager).ToArray();

        var shifts = await db.Shifts
            .AsNoTracking()
            .Where(s => s.AssignedUserId == null
                        && s.IsPublished
                        && workable.Contains(s.Position)
                        && s.Date >= week
                        && s.Date <= weekEnd)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        var result = new List<OpenShiftDto>();

        foreach (var shift in shifts.Where(s => ShiftSpan.From(s.Date, s.StartTime, s.EndTime).Start > now))
        {
            var conflicts = await conflictService.CheckAsync(userId, ToSlot(shift));
            result.Add(new OpenShiftDto(ToDto(shift), conflicts));
        }

        return result;
    }

    public async Task<ServiceResult> ClaimAsync(string userId, int shiftId)
    {
        var now = timeProvider.GetLocalNow().DateTime;

        await using var db = await dbFactory.CreateDbContextAsync();

        var employee = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive, u.Position })
            .FirstOrDefaultAsync();

        if (employee is null)
        {
            return ServiceResult.Failure("Employee not found.");
        }

        var shift = await db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId);
        if (shift is null)
        {
            return ServiceResult.Failure("This shift no longer exists.");
        }

        var isManager = PositionRules.IsManager(employee.Position, await db.IsInManagerRoleAsync(userId));
        var errors = OpenShiftRules.CanClaim(employee.IsActive, employee.Position, isManager, ToDto(shift), now);
        if (errors.Count > 0)
        {
            return ServiceResult.Failure(errors);
        }

        var conflicts = await conflictService.CheckAsync(userId, ToSlot(shift));
        if (conflicts.Count > 0)
        {
            return ServiceResult.Failure(conflicts.Select(c => c.Message));
        }

        var changed = await db.Shifts
            .Where(s => s.Id == shiftId && s.AssignedUserId == null && s.IsPublished)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.AssignedUserId, userId));

        return changed == 1
            ? ServiceResult.Success()
            : ServiceResult.Failure("Sorry, that shift was just taken.");
    }

    private static ShiftSlot ToSlot(Shift shift) =>
        new(shift.Id, shift.Date, shift.StartTime, shift.EndTime);

    private static ShiftDto ToDto(Shift shift) =>
        new(
            shift.Id,
            shift.Date,
            shift.StartTime,
            shift.EndTime,
            shift.Position,
            shift.AssignedUserId,
            null,
            shift.Notes,
            shift.IsPublished);
}
