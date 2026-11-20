using System.ComponentModel.DataAnnotations;
using SwapShop.Core.Enums;

namespace SwapShop.Core.Models;

public class EmployeeFormModel
{
    public string? Id { get; set;}

    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public Position Position { get; set;}

    [Range(0, 500, ErrorMessage = "Hourly rate must be between 0 and 500.")]
    public decimal HourlyRate { get; set;}

    public string? Password { get; set;}
}