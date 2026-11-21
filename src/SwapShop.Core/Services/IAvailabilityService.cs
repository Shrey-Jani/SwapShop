using SwapShop.Core.Models;

namespace SwapShop.Core.Services;

public interface IAvailabilityService
{
    Task<IReadOnlyList<AvailabilityWindow>> GetForUserAsync(string userId);
    Task<ServiceResult> SaveAsync(string userId, IReadOnlyList<AvailabilityWindow> windows);
}
