using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Entities;
using SwapShop.Core.Models;
using SwapShop.Core.Rules;
using SwapShop.Core.Services;

namespace SwapShop.Data;

public class AvailabilityService(IDbContextFactory<ApplicationDbContext> dbFactory) : IAvailabilityService
{
    public async Task<IReadOnlyList<AvailabilityWindow>> GetForUserAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.Availabilities
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .Select(a => new AvailabilityWindow(a.DayOfWeek, a.StartTime, a.EndTime))
            .ToListAsync();
    }

    public async Task<ServiceResult> SaveAsync(string userId, IReadOnlyList<AvailabilityWindow> windows)
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
            .Where(a => a.UserId == userId)
            .ToListAsync();

        db.Availabilities.RemoveRange(existing);

        db.Availabilities.AddRange(windows.Select(w => new Availability
        {
            UserId = userId,
            DayOfWeek = w.Day,
            StartTime = w.Start,
            EndTime = w.End
        }));

        await db.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
