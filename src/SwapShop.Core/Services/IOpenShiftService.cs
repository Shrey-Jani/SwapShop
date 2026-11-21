using SwapShop.Core.Models;

namespace SwapShop.Core.Services;

public interface IOpenShiftService
{
    Task<IReadOnlyList<OpenShiftDto>> GetOpenShiftsAsync(string userId, DateOnly weekStart);

    Task<ServiceResult> ClaimAsync(string userId, int shiftId);
}
