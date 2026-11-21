using SwapShop.Core.Enums;

namespace SwapShop.Core.Models;

public record EmployeeWeekAvailability(
    string EmployeeId,
    string EmployeeName,
    AvailabilitySource Source,
    IReadOnlyList<AvailabilityWindow> Windows);
