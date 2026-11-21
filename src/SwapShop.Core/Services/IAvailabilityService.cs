using SwapShop.Core.Models;

namespace SwapShop.Core.Services;

public interface IAvailabilityService
{
    Task<IReadOnlyList<AvailabilityWindow>> GetStandingAsync(string userId);
    Task<ServiceResult> SaveStandingAsync(string userId, IReadOnlyList<AvailabilityWindow> windows);

    Task<IReadOnlyList<AvailabilityWindow>> GetWeekOverrideAsync(string userId, DateOnly weekStart);
    Task<ServiceResult> SaveWeekOverrideAsync(string userId, DateOnly weekStart, IReadOnlyList<AvailabilityWindow> windows);

    Task<WeekAvailability> GetEffectiveAsync(string userId, DateOnly weekStart);
    Task<IReadOnlyList<EmployeeWeekAvailability>> GetOverviewAsync(DateOnly weekStart);
}
