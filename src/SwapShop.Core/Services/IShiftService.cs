using SwapShop.Core.Models;

namespace SwapShop.Core.Services;

public interface IShiftService
{
    Task<IReadOnlyList<ShiftDto>> GetWeekAsync(DateOnly weekStart);
    Task<ShiftFormModel?> GetForEditAsync(int id);
    Task<ServiceResult> SaveAsync(ShiftFormModel model);
    Task<ServiceResult> DeleteAsync(int id);
    Task<int> PublishWeekAsync(DateOnly weekStart);
}