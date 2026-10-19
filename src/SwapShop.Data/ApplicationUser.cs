using Microsoft.AspNetCore.Identity;
using SwapShop.Core.Enums;

namespace SwapShop.Data;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public Position Position { get; set; }

    public decimal HourlyRate { get; set; }

    public bool IsActive { get; set; } = true;
    
} 