using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Entities;
using SwapShop.Core.Enums;
using SwapShop.Core.Models;
using SwapShop.Core.Rules;
using SwapShop.Core.Services;

namespace SwapShop.Data;

public class TimeOffService(IDbContextFactory<ApplicationDbContext> dbFactory, TimeProvider timeProvider)
    : ITimeOffService
{
    private const int MaxReasonLength = 500;

    public async Task<IReadOnlyList<TimeOffRequestDto>> GetForUserAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var name = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FirstName + " " + u.LastName)
            .FirstOrDefaultAsync() ?? string.Empty;

        var rows = await db.TimeOffRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAtUtc)
            .ToListAsync();

        return rows
            .Select(r => new TimeOffRequestDto(
                r.Id,
                r.UserId,
                name,
                r.StartDate,
                r.EndDate,
                r.Reason,
                r.Status,
                r.IsShortNotice,
                DateTime.SpecifyKind(r.RequestedAtUtc, DateTimeKind.Utc),
                r.ReviewedAtUtc is null ? null : DateTime.SpecifyKind(r.ReviewedAtUtc.Value, DateTimeKind.Utc)))
            .ToList();
    }

    public async Task<ServiceResult> RequestAsync(string userId, TimeOffFormModel model)
    {
        if (model.Reason is { Length: > MaxReasonLength })
        {
            return ServiceResult.Failure($"The reason must be {MaxReasonLength} characters or fewer.");
        }

        var now = timeProvider.GetLocalNow().DateTime;
        var today = DateOnly.FromDateTime(now);

        await using var db = await dbFactory.CreateDbContextAsync();

        if (!await db.Users.AnyAsync(u => u.Id == userId && u.IsActive))
        {
            return ServiceResult.Failure("Employee not found or inactive.");
        }

        var existing = await db.TimeOffRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId
                        && (r.Status == TimeOffStatus.Pending || r.Status == TimeOffStatus.Approved))
            .Select(r => new DateRange(r.StartDate, r.EndDate))
            .ToListAsync();

        var errors = TimeOffRules.Validate(model.StartDate, model.EndDate, today, existing);
        if (errors.Count > 0)
        {
            return ServiceResult.Failure(errors);
        }

        var earliestShiftDate = model.StartDate.AddDays(-1);
        var latestShiftDate = model.EndDate < today.AddDays(1) ? model.EndDate : today.AddDays(1);

        var nearbyShifts = await db.Shifts
            .AsNoTracking()
            .Where(s => s.AssignedUserId == userId
                        && s.Date >= earliestShiftDate
                        && s.Date <= latestShiftDate)
            .ToListAsync();

        var spans = nearbyShifts
            .Select(s => ShiftSpan.From(s.Date, s.StartTime, s.EndTime))
            .ToList();

        db.TimeOffRequests.Add(new TimeOffRequest
        {
            UserId = userId,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Reason = string.IsNullOrWhiteSpace(model.Reason) ? null : model.Reason.Trim(),
            Status = TimeOffStatus.Pending,
            IsShortNotice = TimeOffRules.IsShortNotice(spans, model.StartDate, model.EndDate, now),
            RequestedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        });

        await db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelAsync(string userId, int requestId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var changed = await db.TimeOffRequests
            .Where(r => r.Id == requestId
                        && r.UserId == userId
                        && r.Status == TimeOffStatus.Pending)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.Status, TimeOffStatus.Cancelled));

        return changed == 1
            ? ServiceResult.Success()
            : ServiceResult.Failure("This request cannot be cancelled. It may already have been reviewed.");
    }
}
