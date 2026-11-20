using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Services;
using SwapShop.Core.Models;
using SwapShop.Core.Entities;

namespace SwapShop.Data;

public class ShiftService(IDbContextFactory<ApplicationDbContext> dbFactory) : IShiftService
{
    public async Task<IReadOnlyList<ShiftDto>> GetWeekAsync(DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);

        await using var db = await dbFactory.CreateDbContextAsync();

        var shifts = await db.Shifts
            .AsNoTracking()
            .Where(s => s.Date >= weekStart && s.Date <= weekEnd)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        var userIds = shifts
            .Where(s => s.AssignedUserId is not null)
            .Select(s => s.AssignedUserId!)
            .Distinct()
            .ToList();

        var names = await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);

        return shifts
            .Select(s => new ShiftDto(
                s.Id, s.Date, s.StartTime, s.EndTime, s.Position,
                s.AssignedUserId,
                s.AssignedUserId is not null && names.TryGetValue(s.AssignedUserId, out var name) ? name : null,
                s.Notes, s.IsPublished))
            .ToList();
    }  

    public async Task<ShiftFormModel?> GetForEditAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var shift = await db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (shift is null)
        {
            return null;
        }

        return new ShiftFormModel
        {
            Id = shift.Id,
            Date = shift.Date,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            Position = shift.Position,
            AssignedUserId = shift.AssignedUserId,
            Notes = shift.Notes
        };
    } 

    public async Task<ServiceResult> SaveAsync(ShiftFormModel model)
    {
        if (model.StartTime == model.EndTime)
        {
            return ServiceResult.Failure("Start and end time cannot be the same.");
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        if (model.AssignedUserId is not null)
        {
            var employeeOk = await db.Users.AnyAsync(u => u.Id == model.AssignedUserId && u.IsActive);
            if (!employeeOk)
            {
                return ServiceResult.Failure("The selected employee does not exist or is inactive.");
            }
        }

        Shift shift;
        if (model.Id is null)
        {
            shift = new Shift();
            db.Shifts.Add(shift);
        }
        else
        {
            var existing = await db.Shifts.FindAsync(model.Id.Value);
            if (existing is null)
            {
                return ServiceResult.Failure("Shift not found.");
            }

            shift = existing;
        }

        shift.Date = model.Date;
        shift.StartTime = model.StartTime;
        shift.EndTime = model.EndTime;
        shift.Position = model.Position;
        shift.AssignedUserId = model.AssignedUserId;
        shift.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();

        await db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var rows = await db.Shifts.Where(s => s.Id == id).ExecuteDeleteAsync();
        return rows == 0 ? ServiceResult.Failure("Shift not found.") : ServiceResult.Success();
    }

    public async Task<int> PublishWeekAsync(DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);

        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.Shifts
            .Where(s => s.Date >= weekStart && s.Date <= weekEnd && !s.IsPublished)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsPublished, true));
    }
}