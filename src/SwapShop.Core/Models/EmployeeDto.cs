using SwapShop.Core.Enums;

namespace SwapShop.Core.Models;

public record EmployeeDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    Position Position,
    decimal HourlyRate,
    bool IsActive
)
{
    public string FullName => $"{FirstName} {LastName}";
}