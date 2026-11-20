using Microsoft.AspNetCore.Identity;

namespace SwapShop.Data;

public class ActiveUserConfirmation : IUserConfirmation<ApplicationUser>
{
    public Task<bool> IsConfirmedAsync(UserManager<ApplicationUser> manager, ApplicationUser user) => Task.FromResult(user.EmailConfirmed && user.IsActive);
    
}