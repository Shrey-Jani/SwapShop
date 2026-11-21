using SwapShop.Core.Models;

namespace SwapShop.Core.Services;

public interface ITimeOffService
{
    Task<IReadOnlyList<TimeOffRequestDto>> GetForUserAsync(string userId);
    Task<ServiceResult> RequestAsync(string userId, TimeOffFormModel model);
    Task<ServiceResult> CancelAsync(string userId, int requestId);
}
