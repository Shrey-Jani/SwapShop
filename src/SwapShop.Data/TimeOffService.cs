using Microsoft.EntityFrameworkCore;
using SwapShop.Core.Constants;
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

        var rows = await db.TimeOffRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAtUtc)
            .ToListAsync();

        var names = await LoadNamesAsync(db, [userId]);

        return rows.Select(r => ToDto(r, names)).ToList();
    }

    public async Task<IReadOnlyList<TimeOffRequestDto>> GetPendingAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var rows = await db.TimeOffRequests
            .AsNoTracking()
            .Where(r => r.Status == TimeOffStatus.Pending)
            .OrderByDescending(r => r.IsShortNotice)
            .ThenBy(r => r.StartDate)
            .ThenBy(r => r.RequestedAtUtc)
            .ToListAsync();

        if (rows.Count == 0)
        {
            return [];
        }

        var userIds = rows.Select(r => r.UserId).Distinct().ToList();
        var names = await LoadNamesAsync(db, userIds);

        var earliest = rows.Min(r => r.StartDate).AddDays(-1);
        var latest = rows.Max(r => r.EndDate);

        var shifts = await db.Shifts
            .AsNoTracking()
            .Where(s => s.AssignedUserId != null
                        && userIds.Contains(s.AssignedUserId)
                        && s.Date >= earliest
                        && s.Date <= latest)
            .ToListAsync();

        var spansByUser = shifts
            .GroupBy(s => s.AssignedUserId!)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => ShiftSpan.From(s.Date, s.StartTime, s.EndTime)).ToList());

        return rows
            .Select(r =>
            {
                var affected = spansByUser.TryGetValue(r.UserId, out var spans)
                    ? TimeOffRules.CountAffectedShifts(spans, r.StartDate, r.EndDate)
                    : 0;

                return ToDto(r, names, affected);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<TimeOffRequestDto>> GetHistoryAsync(int take = 50)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var rows = await db.TimeOffRequests
            .AsNoTracking()
            .Where(r => r.Status != TimeOffStatus.Pending)
            .OrderByDescending(r => r.ReviewedAtUtc ?? r.RequestedAtUtc)
            .Take(take)
            .ToListAsync();

        var names = await LoadNamesAsync(db, rows.Select(r => r.UserId).Distinct().ToList());

        return rows.Select(r => ToDto(r, names)).ToList();
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

    public async Task<ServiceResult> ReviewAsync(int requestId, string managerId, bool approve)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var isManager = await (
            from userRole in db.UserRoles
            join role in db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == managerId && role.Name == Roles.Manager
            select userRole).AnyAsync();

        if (!isManager)
        {
            return ServiceResult.Failure("Only a manager can review time-off requests.");
        }

        var newStatus = approve ? TimeOffStatus.Approved : TimeOffStatus.Denied;
        DateTime? reviewedAt = timeProvider.GetUtcNow().UtcDateTime;

        var changed = await db.TimeOffRequests
            .Where(r => r.Id == requestId && r.Status == TimeOffStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, newStatus)
                .SetProperty(r => r.ReviewedByUserId, managerId)
                .SetProperty(r => r.ReviewedAtUtc, reviewedAt));

        return changed == 1
            ? ServiceResult.Success()
            : ServiceResult.Failure("This request was already reviewed or cancelled. Refresh the page.");
    }

    private static async Task<Dictionary<string, string>> LoadNamesAsync(
        ApplicationDbContext db, IReadOnlyCollection<string> userIds) =>
        await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);

    private static TimeOffRequestDto ToDto(
        TimeOffRequest request, Dictionary<string, string> names, int affectedShifts = 0) =>
        new(
            request.Id,
            request.UserId,
            names.GetValueOrDefault(request.UserId, string.Empty),
            request.StartDate,
            request.EndDate,
            request.Reason,
            request.Status,
            request.IsShortNotice,
            DateTime.SpecifyKind(request.RequestedAtUtc, DateTimeKind.Utc),
            request.ReviewedAtUtc is null ? null : DateTime.SpecifyKind(request.ReviewedAtUtc.Value, DateTimeKind.Utc),
            affectedShifts);
}
