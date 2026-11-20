using SwapShop.Core.Models;

namespace SwapShop.Core.Services;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync(bool includeInactive = true);
    Task<EmployeeFormModel?> GetForEditAsync(string id);
    Task<ServiceResult> CreateAsync(EmployeeFormModel model);
    Task<ServiceResult> UpdateAsync(EmployeeFormModel model);

    Task<ServiceResult> SetActiveAsync(string id, bool isActive);
}